using UnityEngine;
using Unity.Netcode;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;

    public float distance = 6f;
    public float height = 2f;
    public float mouseSensitivity = 200f;

    float currentYaw = 0f;
    float currentPitch = 15f;

    private static ThirdPersonCamera instance;
    private float shakeTimer = 0f;
    private float totalShakeDuration = 0.001f;
    private float shakeMagnitude = 0f;

    void Awake()
    {
        instance = this;
    }

    public static void Shake(float duration, float magnitude)
    {
        float cappedMag = Mathf.Clamp(magnitude, 0.005f, 0.055f);
        if (instance != null)
        {
            instance.shakeTimer = Mathf.Max(instance.shakeTimer, duration);
            instance.totalShakeDuration = Mathf.Max(instance.totalShakeDuration, duration);
            instance.shakeMagnitude = Mathf.Max(instance.shakeMagnitude, cappedMag);
        }
        else
        {
            ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
            if (cam != null)
            {
                instance = cam;
                instance.shakeTimer = Mathf.Max(instance.shakeTimer, duration);
                instance.totalShakeDuration = Mathf.Max(instance.totalShakeDuration, duration);
                instance.shakeMagnitude = Mathf.Max(instance.shakeMagnitude, cappedMag);
            }
        }
    }

    void Start()
    {
        Camera mainCam = GetComponent<Camera>();
        if (mainCam != null)
        {
            mainCam.targetDisplay = 0; // Primary Display 1

            // Ensure Display 2 also has a seamless rendering camera if the user selects Display 2 in Unity Editor
            Transform aux = transform.Find("AuxDisplay2Camera");
            if (aux == null)
            {
                GameObject auxObj = new GameObject("AuxDisplay2Camera");
                auxObj.transform.SetParent(transform, false);
                Camera auxCam = auxObj.AddComponent<Camera>();
                auxCam.CopyFrom(mainCam);
                auxCam.targetDisplay = 1; // Display 2 in Unity
            }
        }
    }

    void Update()
    {
        // ابحث فقط عن اللاعب الذي يملكه هذا الجهاز
        if (target == null)
        {
            FindLocalPlayer();
        }
    }

    void FindLocalPlayer()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && NetworkManager.Singleton.SpawnManager != null)
        {
            NetworkObject localPlayer =
                NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

            if (localPlayer != null)
            {
                target = localPlayer.transform;
                return;
            }
        }

        // Fallback: ابحث عن اللاعب المالك أو أي لاعب بشري في المشهد
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>();
        foreach (var p in players)
        {
            if (p.IsOwner || (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening))
            {
                target = p.transform;
                break;
            }
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // أثناء فتح شاشة اختيار الشخصية لا ندور الكاميرا
        if (CharacterSelectionUI.IsSelectionOpen)
            return;

        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity *
            Time.deltaTime;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity *
            Time.deltaTime;

        currentYaw += mouseX;
        currentPitch -= mouseY;

        currentPitch =
            Mathf.Clamp(currentPitch, -30f, 60f);

        Quaternion rotation =
            Quaternion.Euler(
                currentPitch,
                currentYaw,
                0f
            );

        Vector3 focusPoint = target.position + Vector3.up * height;
        Vector3 desiredBackDir = -(rotation * Vector3.forward);
        float currentDistance = distance;

        // فحص الاصطدام لمنع الكاميرا من الدخول تحت الأرض أو داخل الجدران والمنحدرات
        int obstacleMask = ~LayerMask.GetMask("Player", "Ignore Raycast");
        if (Physics.SphereCast(focusPoint, 0.28f, desiredBackDir, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            currentDistance = Mathf.Clamp(hit.distance - 0.15f, 0.8f, distance);
        }

        Vector3 position = focusPoint + desiredBackDir * currentDistance;

        // حماية تامة من النزول تحت سطح الأرض
        if (position.y < 0.5f)
        {
            position.y = 0.5f;
        }

        transform.position = position;

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float decay = Mathf.Clamp01(shakeTimer / Mathf.Max(0.001f, totalShakeDuration));
            decay = decay * decay; // Smooth quadratic falloff
            Vector3 shakeOffset = Random.insideUnitSphere * (shakeMagnitude * decay);
            transform.position += shakeOffset;
        }
        else
        {
            shakeMagnitude = 0f;
            totalShakeDuration = 0.001f;
        }

        transform.LookAt(
            target.position +
            Vector3.up * 1.5f
        );
    }
}
