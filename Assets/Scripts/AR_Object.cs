using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AR_Object : MonoBehaviour
{
    [Header("Tracking Stability Settings")]
    [Tooltip("Time in seconds before deactivating on limited tracking")]
    public float errorThreshold = 1.5f;
    
    [Tooltip("Time in seconds before deactivating on lost tracking")]
    public float nonTrackThreshold = 1.0f;
    
    [Tooltip("Time in seconds to smoothly transition when tracking is regained")]
    public float reappearanceBlendTime = 0.5f;
    
    [Tooltip("Speed to move toward target position when tracking is good")]
    public float trackingLerpSpeed = 10f;
    
    [Tooltip("Speed to move toward target position when tracking is limited")]
    public float limitedTrackingLerpSpeed = 5f;
    
    [Tooltip("Minimum distance (in meters) required to update position")]
    public float deadZoneThreshold = 0.005f;
    
    [Tooltip("Maximum position jump (in meters) allowed when reacquiring tracking")]
    public float maxReacquireJump = 0.2f;
    
    // Internal tracking timers
    private float errorTimer = 0f;
    private float nonTrackTimer = 0f;
    private float reappearanceBlendFactor = 1f;
    
    // Tracking state management
    private bool wasTrackingLost = false;
    private float timeSinceTrackingLost = 0f;
    private string previousTrackingState = "None";
    
    // Last known good position and rotation
    private Vector3 lastGoodPosition;
    private Quaternion lastGoodRotation;
    
    // Debug display
    [Header("Debug")]
    [SerializeField] private string trackingState = "None";
    [SerializeField] private float currentErrorTime = 0f;
    [SerializeField] private float currentNonTrackTime = 0f;
    [SerializeField] private bool isReappearanceBlending = false;
    [SerializeField] private float jumpDistance = 0f;
    
    public void OnEnable()
    {
        // Reset timers when object is enabled
        errorTimer = 0f;
        nonTrackTimer = 0f;
        reappearanceBlendFactor = 1f;
        wasTrackingLost = false;
        timeSinceTrackingLost = 0f;
        trackingState = "None";
        previousTrackingState = "None";
        
        // Initialize position/rotation tracking
        lastGoodPosition = transform.position;
        lastGoodRotation = transform.rotation;
    }
    
    public void Update()
    {
        // Gradually decrease timers if they're above zero (recovery)
        if (errorTimer > 0) 
        {
            errorTimer -= Time.deltaTime * 0.5f; // Recover at half speed
            errorTimer = Mathf.Max(0, errorTimer); // Prevent negative values
        }
        
        if (nonTrackTimer > 0)
        {
            nonTrackTimer -= Time.deltaTime * 0.5f;
            nonTrackTimer = Mathf.Max(0, nonTrackTimer);
        }
        
        // If tracking was lost, increase the time counter
        if (wasTrackingLost)
        {
            timeSinceTrackingLost += Time.deltaTime;
        }
        
        // Handle reappearance blending
        if (isReappearanceBlending)
        {
            reappearanceBlendFactor += Time.deltaTime / reappearanceBlendTime;
            if (reappearanceBlendFactor >= 1f)
            {
                reappearanceBlendFactor = 1f;
                isReappearanceBlending = false;
            }
        }
        
        // Update debug values
        currentErrorTime = errorTimer;
        currentNonTrackTime = nonTrackTimer;
    }
    
    /// <summary>
    /// Updates the position and rotation of this AR object based on target transform
    /// </summary>
    /// <param name="targetPosition">Target position from AR tracking</param>
    /// <param name="targetRotation">Target rotation from AR tracking</param>
    /// <param name="quality">Current tracking quality state</param>
    public void UpdateTransform(Vector3 targetPosition, Quaternion targetRotation, string quality)
    {
        // Track state changes
        bool justReacquiredTracking = (quality == "Tracking" && (previousTrackingState == "None" || previousTrackingState == "Limited") && wasTrackingLost);
        
        // Calculate jump distance if we just reacquired tracking
        if (justReacquiredTracking)
        {
            jumpDistance = Vector3.Distance(transform.position, targetPosition);
            
            // If the jump is too large after a short tracking loss, it might be a false positive
            if (jumpDistance > maxReacquireJump && timeSinceTrackingLost < 1.0f)
            {
                // Use lastGoodPosition instead, but start blending toward the new position
                targetPosition = Vector3.Lerp(lastGoodPosition, targetPosition, 0.1f);
                targetRotation = Quaternion.Slerp(lastGoodRotation, targetRotation, 0.1f);
                isReappearanceBlending = true;
                reappearanceBlendFactor = 0.1f;
            }
            else if (jumpDistance > deadZoneThreshold)
            {
                // Start blending if there's a significant jump
                isReappearanceBlending = true;
                reappearanceBlendFactor = 0f;
            }
            
            // Reset tracking lost state
            wasTrackingLost = false;
            timeSinceTrackingLost = 0f;
        }
        
        trackingState = quality;
        
        switch (quality)
        {
            case "Tracking":
                // Check if movement is beyond dead zone
                if (Vector3.Distance(transform.position, targetPosition) > deadZoneThreshold || justReacquiredTracking)
                {
                    float lerpRate = isReappearanceBlending ? 
                        Mathf.Lerp(2f, trackingLerpSpeed, reappearanceBlendFactor) : 
                        trackingLerpSpeed;
                    
                    // Smoothly move toward target position
                    transform.position = Vector3.Lerp(
                        transform.position, 
                        targetPosition, 
                        Time.deltaTime * lerpRate);
                    
                    // Smoothly rotate toward target rotation
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        Time.deltaTime * lerpRate);
                    
                    // Store good position/rotation
                    lastGoodPosition = transform.position;
                    lastGoodRotation = transform.rotation;
                }
                
                // Clear error state
                RidError();
                break;
                
            case "Limited":
                if (Vector3.Distance(transform.position, targetPosition) > deadZoneThreshold)
                {
                    // Still move but more slowly when tracking is limited
                    transform.position = Vector3.Lerp(
                        transform.position, 
                        targetPosition, 
                        Time.deltaTime * limitedTrackingLerpSpeed);
                    
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        Time.deltaTime * limitedTrackingLerpSpeed);
                }
                
                // If we just went from Tracking to Limited, mark the tracking as potentially lost
                if (previousTrackingState == "Tracking" && !wasTrackingLost)
                {
                    wasTrackingLost = true;
                    timeSinceTrackingLost = 0f;
                }
                break;
                
            case "None":
                // When no tracking, we don't update position/rotation
                
                // If we just went from Tracking/Limited to None, mark the tracking as lost
                if ((previousTrackingState == "Tracking" || previousTrackingState == "Limited") && !wasTrackingLost)
                {
                    wasTrackingLost = true;
                    timeSinceTrackingLost = 0f;
                }
                break;
        }
        
        // Remember the current state for next frame
        previousTrackingState = quality;
    }
    
    /// <summary>
    /// Adds error time when tracking is limited
    /// </summary>
    /// <param name="deltaTime">Time since last frame</param>
    /// <returns>True if error threshold exceeded</returns>
    public bool AddError(float deltaTime)
    {
        errorTimer += deltaTime;
        return errorTimer > errorThreshold;
    }
    
    /// <summary>
    /// Adds non-tracking time when tracking is lost
    /// </summary>
    /// <param name="deltaTime">Time since last frame</param>
    /// <returns>True if non-tracking threshold exceeded</returns>
    public bool AddKillStrike(float deltaTime)
    {
        nonTrackTimer += deltaTime;
        return nonTrackTimer > nonTrackThreshold;
    }
    
    /// <summary>
    /// Resets error and non-tracking timers
    /// </summary>
    public void RidError()
    {
        errorTimer = 0f;
        nonTrackTimer = 0f;
    }
    
    /// <summary>
    /// Restore the last known good position and rotation
    /// </summary>
    public void RestoreLastGoodTransform()
    {
        transform.position = lastGoodPosition;
        transform.rotation = lastGoodRotation;
    }
}