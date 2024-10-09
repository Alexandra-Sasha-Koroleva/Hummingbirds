using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// A hummingbird Machine Learning Agent 
/// </summary>
public class HummingbirdAgent : Agent
{
   [Tooltip("Force to apply when moving")]
   public float moveForce = 2f;
   
   [Tooltip("Speed to pitch up or down")]
   public float pitchSpeed = 100f;
   
   [Tooltip("Speed to rotate around the up axis")]
   public float yawSpeed = 100f;

   [Tooltip("Transform at the tip of the beak")]
   public Transform beakTip;

   [Tooltip("The agent's camera")] 
   public Camera agentCamera;
   
   [Tooltip("Whether this is traing mode or gameplay mode")]
   public bool traingMode;
   
   // The rigid body of the agent
   private Rigidbody _rigidbody;
   
   // The flower area that the agent is in
   private FlowerArea _flowerArea;
   
   // The nearest flower to the agent
   private Flower _nearestFlower;
   
   // Allows for smoother pitch changes
   private float _smoothPitchChangge = 0f;
   
   // Allows for smoother yaw changes
   private float _smoothYawChangge = 0f;
   
   // Maximum angle that the bird can pitch up of down
   private const float MaxPitchAngle = 80f;
   
   // Maximum distance from the beak tip to accept nectar collision
   private const float BeakTipRadius = 0.008f;
   
   // Whether the agent is frozen (intentionally not flying)
   private bool frozen = false;
   
   /// <summary>
   /// The amount of nectar the agent has obtained this episode
   /// </summary>
   public float NectarObtained{get; private set;}
   

   /// <summary>
   /// Initialize the agent
   /// </summary>
   public override void Initialize()
   {
      _rigidbody = GetComponent<Rigidbody>();
      _flowerArea = GetComponentInParent<FlowerArea>();
      
      // If not training mode, no max step, play forever
      if (!traingMode) MaxStep = 0; 
   }

   /// <summary>
   /// Reset the agent when an episode begins
   /// </summary>
   public override void OnEpisodeBegin()
   {
      if (traingMode)
      {
         // Only reset flowers in training when there is one agent per area
         _flowerArea.ResetFlowers();
      }
      
      // Reset nectar obtained
      NectarObtained = 0f;
      
      // zero out velocities so that movement stops before a new episode begind
      _rigidbody.velocity = Vector3.zero;
      _rigidbody.angularVelocity = Vector3.zero;
      
      // Default to spawning in front of a flower
      bool inFrontOfFlower = true;
      if (traingMode)
      {
         // Spawn in front of flower 50% of the time during training
         inFrontOfFlower = UnityEngine.Random.value > 0.5f;
      }
      
      // Move agent to a new random position
      MoveToSafeRandomPosition(inFrontOfFlower);
         
      // Recalculate the nearest flower now that the agent has moved
      UpdateNearestFlower();
   }

   /// <summary>
   /// Move the agent to a safe random positon (i.e. doesn't collide w/ anything
   /// If in front of flower, also point the beak at flower
   /// </summary>
   /// <param name="inFrontOfFlower">Whether to choose a spot in front of a flower</param>
   /// <exception cref="NotImplementedException"></exception>
   private void MoveToSafeRandomPosition(bool inFrontOfFlower)
   {
      bool safePositionFound = false;
      int attemptsRemaining = 100; // Prevent an infinite loop
      Vector3 potentialPosition = Vector3.zero;
      Quaternion potentialRotation = new Quaternion();
      
      // Loop until a sage position is found or we run out of attempts
      while (!safePositionFound && attemptsRemaining > 0)
      {
         attemptsRemaining--;
         if (inFrontOfFlower)
         {
            // Pick a random flower
            Flower randomFlower = _flowerArea.Flowers[UnityEngine.Random.Range(0, _flowerArea.Flowers.Count)];
            
            // Position 10-20 cm in front of the flower
            float distanceFromFlower = UnityEngine.Random.Range(.1f, .2f);
            potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;
            
            // Point beak at flower (bird's head is center of transform)
            Vector3 toFlower = randomFlower.FlowerUpVector - potentialPosition;
            potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
         }
         else
         {
            // Pick a random height from the ground
            float height = UnityEngine.Random.Range(1.2f, 2.5f);
            
            // Pick a random radius from the center of the area
            float radius = UnityEngine.Random.Range(2f, 7f);
            
            // Pick a random direction rotated around the y axis
            Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);
            
            // combine height, radius, and direction to pick a potential position
            potentialPosition = _flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;
            
            // choose and set random starting pitch and yaw
            float pitch = UnityEngine.Random.Range(-60f, 60f);
            float yaw = UnityEngine.Random.Range(-180f, 180f);
            potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            
         }
         
         // Check to see if the agent will collide with anything
         Collider[] colliders = Physics.OverlapSphere(potentialPosition, .05f);
         
         // Safe postion has been found if no colliders are overlapped
         safePositionFound = colliders.Length == 0;
      }
      Debug.Assert(safePositionFound, "Couldn't find a safe position to spawn");
      
      //Set position and rotation
      transform.position = potentialPosition;
      transform.rotation = potentialRotation;
   }

   /// <summary>
   /// Update the nearest flower to the agent
   /// </summary>
   private void UpdateNearestFlower()
   {
      foreach (Flower flower in _flowerArea.Flowers)
      {
         if (_nearestFlower == null && flower.HasNectar)
         {
            // No current nearest flower and this flower has nectar, so set to this flower 
            _nearestFlower = flower;
         }
         else if (flower.HasNectar)
         {
            //Calculate distance to this flower and distance to the curent nearest flower
            float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
            float distanceToCurrentNearestFlower = Vector3.Distance(_nearestFlower.transform.position, beakTip.position);
            
            // If current nearest flower is empty OR this flower is closer, update the nearest flower
            if (!_nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
            {
               _nearestFlower = flower;
            }
         }
            
      }
   }
}
