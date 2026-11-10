using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Victory : MonoBehaviour
{
    [FormerlySerializedAs("doorPanel")]
    [FormerlySerializedAs("doorSprite")]
    [SerializeField] Transform doorDash;

    bool playerAtDoor;
    bool isOpening;
     bool isDoorOpen;
    bool levelEnded;
    [SerializeField] GameObject enterText;

    void OnTriggerEnter2D(Collider2D hit)
    {
        if (!hit.CompareTag("Player")) return;

        playerAtDoor = true;

        if (!isOpening &&!isDoorOpen)
        {
            isOpening = true;
               StartCoroutine(OpenDoor());
        }
    }

    void OnTriggerExit2D(Collider2D hit)
    {
        if (hit.CompareTag("Player"))
            playerAtDoor = false;
    }

    void Update()
    {
        if (LightPlatformer.UI.GameMenuController.GameplayInputBlocked) return;
        if (enterText != null) enterText.SetActive(playerAtDoor && isDoorOpen && !levelEnded);
        if (!levelEnded && playerAtDoor && isDoorOpen && Input.GetKeyDown(KeyCode.E))
        {
            levelEnded = true;
            if (enterText != null) enterText.SetActive(false);
            LightPlatformer.UI.GameMenuController.NotifyVictory();
            
        
        
        
        }
    }

    IEnumerator OpenDoor()
    {
        if (doorDash == null)
        {
            isOpening = false;
            isDoorOpen = true;
            yield break;
        }

        Vector3 startPos = doorDash.localPosition;
        Vector3 startScale = doorDash.localScale;
        float halfDoorWidth = doorDash.GetComponent<SpriteRenderer>().sprite.bounds.size.x
                              *startScale.x *0.5f;

        for (float openTime = 0f; openTime < 0.4f; openTime += Time.deltaTime)
        {
            float openAmount = Mathf.SmoothStep(0f, 1f, openTime / 0.4f);

            doorDash.localScale = new Vector3(
                startScale.x * (1f- openAmount), startScale.y, startScale.z);
                doorDash.localPosition = startPos + Vector3.left *halfDoorWidth * openAmount;

            yield return null;
        }

        doorDash.gameObject.SetActive(false);
            isOpening = false;
        isDoorOpen = true;
        while (playerAtDoor && !levelEnded)
        {
            yield return null;
            }

        if (levelEnded) yield break;

            isDoorOpen = false;
        isOpening = true;
        doorDash.gameObject.SetActive(true);

            for (float closeTime = 0f; closeTime < 0.4f; closeTime += Time.deltaTime)
        {
            float closeAmount = Mathf.SmoothStep(0f, 1f, closeTime / 0.4f);

            doorDash.localScale = new Vector3(
                startScale.x * closeAmount, startScale.y, startScale.z);
            doorDash.localPosition = startPos + Vector3.left * halfDoorWidth * (1f - closeAmount);

            yield return null;
        }

         doorDash.localPosition = startPos;
        doorDash.localScale = startScale;
        isOpening = false;

        if (playerAtDoor)
        {
         isOpening = true;
            StartCoroutine(OpenDoor());
        }
    }
}