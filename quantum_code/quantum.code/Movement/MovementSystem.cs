using System;
using System.Diagnostics;
using Photon.Deterministic;
using Quantum;
using Quantum.Prototypes;

namespace Quantum.Movement {
    public unsafe class MovementSystem : SystemMainThreadFilter<MovementSystem.Filter>, ISignalOnPlayerDataSet, ISignalOnPlayerDisconnected {
        public struct Filter {
            public EntityRef Entity;
            public CharacterController3D* CharacterController;
            public Transform3D* Transform;
            public PlayerLink* Link;
            public PlayerAnimationState* PlayerAnimationState;
        }

        public override void Update(Frame f, ref Filter filter) {
            var input = f.GetPlayerInput(filter.Link->Player);
            
            // Handle rotation based on mouse input
            var currentRotation = filter.Transform->Rotation;
            // Calcula el cambio en la rotación basado en el input del mouse y la sensibilidad deseada
            var rotationChange = (FP)input->Rotation * FP.Deg2Rad * f.DeltaTime; // rotationSensitivity es un FP que controla qué tan rápido gira el personaje
            // Aplica el cambio en la rotación directamente a la rotación actual
            var newRotation = currentRotation * FPQuaternion.CreateFromYawPitchRoll(rotationChange, 0, 0);

            // Establece la nueva rotación al transform del personaje
            filter.Transform->Rotation = newRotation;
            
            var forward = filter.Transform->Rotation * FPVector3.Forward * ((filter.CharacterController->Grounded)?1:FP._0_33);
            var right = filter.Transform->Rotation * FPVector3.Right * ((filter.CharacterController->Grounded)?1:FP._0_33);
            var worldInput = forward * (input->DirectionY) + right * (input->DirectionX );

            // Mueve el personaje en el espacio mundial
            filter.CharacterController->Move(f, filter.Entity, worldInput);

            // Handle jump input
            if (input->Jump.WasPressed) {
                Log.Debug("Quantum: Jump pressed");
                filter.CharacterController->Jump(f);
                filter.PlayerAnimationState->IsJumping = true;
            }
            else {
                filter.PlayerAnimationState->IsJumping = false;
            }

            if (input->Shoot.WasPressed) {
                var entityPrototype = f.FindAsset<EntityPrototype>("Resources/DB/ShotPrefab|EntityPrototype");
                var entity = f.Create(entityPrototype);
                // Asumiendo que 'entity' es la referencia de la entidad que acabas de crear

                if (f.Unsafe.TryGetPointer<Transform3D>(entity, out var transformShot)) {
                    // Calcular la posición inicial de la bala
                    FPVector3 playerPosition = filter.Transform->Position;
                    FPQuaternion playerRotation = filter.Transform->Rotation;
                    FPVector3 shotForwardDirection = playerRotation * FPVector3.Forward;  // Vector forward en la dirección del jugador

                    // Calcular la nueva posición sumando la altura de 1 y hacia adelante por 1
                    FPVector3 shotPosition = playerPosition + (shotForwardDirection * FP._1) + new FPVector3(FP._0, FP._0_50, FP._0);

                    transformShot->Position = shotPosition;
                    transformShot->Rotation = playerRotation;  // La bala mira en la misma dirección que el jugador
                }
                filter.PlayerAnimationState->IsShooting = true;
            }
            else {
                filter.PlayerAnimationState->IsShooting = false;
            }

            
            // Check if the player has fallen too far
            if (filter.Transform->Position.Y < -7) {
                Log.Debug("Quantum: player fell too far, respawning");
                filter.Transform->Position = GetSpawnPosition(filter.Link->Player, f.PlayerCount);
            }
            



            if (filter.CharacterController->Grounded ) {
                if (input->DirectionX > FP._0) filter.PlayerAnimationState->IsWalkingX = 1;
                else if(input->DirectionX < FP._0) filter.PlayerAnimationState->IsWalkingX = -1;
                else filter.PlayerAnimationState->IsWalkingX = 0;
                
                if (input->DirectionY > FP._0) filter.PlayerAnimationState->IsWalkingY = 1;
                else if(input->DirectionY < FP._0) filter.PlayerAnimationState->IsWalkingY = -1;
                else filter.PlayerAnimationState->IsWalkingY = 0;
                //Log.Debug("Quantum: X"+input->DirectionX+" Y"+input->DirectionY);
            }
            filter.PlayerAnimationState->IsGrounded = filter.CharacterController->Grounded;
    

        }

        public void OnPlayerDataSet(Frame f, PlayerRef player) {
            var data = f.GetPlayerData(player);
            var prototypeEntity = f.FindAsset<EntityPrototype>(data.CharacterPrototype.Id);
            var createdEntity = f.Create(prototypeEntity);

            if (f.Unsafe.TryGetPointer<PlayerLink>(createdEntity, out var playerLink)) {
                playerLink->Player = player;
            }

            if (f.Unsafe.TryGetPointer<Transform3D>(createdEntity, out var transform)) {
                transform->Position = GetSpawnPosition(player, f.PlayerCount);
            }
            if (f.Unsafe.TryGetPointer<PlayerAnimationState>(createdEntity, out var animState)) {
                // Inicializar estados aquí si es necesario
                animState->IsWalkingX = (short)0f;
                animState->IsWalkingY = (short)0f;
                animState->IsJumping = false;
                animState->IsGrounded = false;
                animState->IsShooting = false;
            }
            
        }

        FPVector3 GetSpawnPosition(int playerNumber, int playerCount) {
            int widthOfAllPlayers = playerCount * 2;
            return new FPVector3(playerNumber * 2 + 1 - widthOfAllPlayers / 2, 0, 0);
        }

        public void OnPlayerDisconnected(Frame f, PlayerRef player) {
            foreach (var playerLink in f.GetComponentIterator<PlayerLink>()) {
                if (playerLink.Component.Player != player) {
                    continue;
                }
                f.Destroy(playerLink.Entity);
            }
        }
    }
    public unsafe class ShotMovementSystem : SystemMainThreadFilter<ShotMovementSystem.Filter> {
        public struct Filter {
            public EntityRef Entity;
            public Transform3D* Transform;
            public Shot* Shot;
        }

        public override void Update(Frame f, ref Filter filter) {
            // Calcular el vector forward basado en la rotación actual del transform
            var forward = filter.Transform->Rotation * FPVector3.Forward;

            // Opcional: ajustar la velocidad de la bala
            FP speed = FP._10 +FP._10 + FP._10 + FP._5; // Velocidad a la que se moverá la bala

            // Actualizar la posición de la bala moviéndola hacia adelante en la dirección de su rotación
            filter.Transform->Position += forward * speed * f.DeltaTime;
        }


    }
}
public class BulletCollisionSystem : SystemSignalsOnly, ISignalOnCollisionEnter3D {

    public void OnCollisionEnter3D(Frame f, CollisionInfo3D info) {
        if (!f.Has<Shot>(info.Entity)) return;
        f.Destroy(info.Entity);
        
        if (!f.Has<Enemy>(info.Other)) return;
        f.Destroy(info.Other);
        int enemies = 0;
        foreach (var (entity, component) in f.GetComponentIterator<Enemy>()) {
            enemies++;
        }
        Log.Debug("Quantum: Enemies: "+enemies);
        
        if (enemies == 0) {
            Log.Debug("Quantum: all enemies destroyed");
            SendGameEndMessage(f);
        }
    }

    private void SendGameEndMessage(Frame f) {
        // Implementar la comunicación hacia Unity para la UI de victoria
        f.Events.GameEndEvent();
    }
}
