using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BotEmotion
{
    Neutral,
    Happy,
    Sad,
    Angry,
    Surprised
}

[System.Serializable]
public struct BlendShapeWeight
{
    public string shapeName;
    [Range(0f, 100f)] public float weight;
}

[System.Serializable]
public struct EmotionProfile
{
    public BotEmotion emotion;
    public BlendShapeWeight[] blendShapes;
}

public partial class WanderBot
{
    [Header("Face System & Blink")]
    [Tooltip("İçinde blendshape'lerin bulunduğu asıl 'Body' objesi")]
    [SerializeField] private SkinnedMeshRenderer faceMesh;
    [SerializeField] private string blinkBlendShapeName = "Inori_Blink";
    [SerializeField] private float minBlinkWait = 2.5f;
    [SerializeField] private float maxBlinkWait = 6.0f;
    [SerializeField] private float blinkSpeed = 0.1f;

    [Header("Emotion System")]
    [Tooltip("Duygular arası geçişin pürüzsüzlük süresi")]
    [SerializeField] private float emotionTransitionDuration = 0.3f;
    [SerializeField] private EmotionProfile[] emotionProfiles;

    [Header("Lip Sync System")]
    [Tooltip("İsteğe Bağlı: Karakterin sesi buradan çıkıyorsa otomatik dudak oynatır")]
    [SerializeField] private AudioSource voiceSource;
    [Tooltip("Konuşurken rastgele kullanılacak dudak şekilleri (VRChat uyumlu)")]
    [SerializeField] private string[] visemeBlendShapes = new string[] {
        "vrc.v_aa", "vrc.v_e", "vrc.v_ih", "vrc.v_oh", "vrc.v_ou"
    };
    [SerializeField] private float lipSyncSpeed = 0.05f;
    [Tooltip("Konuşma içindeki sessizlikleri/nefes almayı anlamak için hassasiyet")]
    [SerializeField] private float voiceVolumeThreshold = 0.005f;

    private bool isFaceSystemActive;
    private int blinkIndex = -1;
    private Coroutine currentEmotionRoutine;
    private BotEmotion currentEmotion = BotEmotion.Neutral;
    private List<int> managedEmotionIndices = new List<int>();

    // Lip Sync Variables
    private bool isTalking = false;
    private List<int> visemeIndices = new List<int>();
    private int currentVisemeIndex = -1;
    private float[] audioSampleBuffer = new float[64]; // Çalan sesi anlık analiz etmek için minik hafıza

    private void SetupFaceSystem()
    {
        if (faceMesh == null)
        {
            Debug.LogWarning("WanderBot: Face Mesh atanmadığı için Yüz Sistemi başlatılamadı!");
            return;
        }

        // --- Göz Kırpma Hazırlığı ---
        blinkIndex = faceMesh.sharedMesh.GetBlendShapeIndex(blinkBlendShapeName);

        // --- Duygu Sistemi Hazırlığı ---
        foreach (var profile in emotionProfiles)
        {
            foreach (var shape in profile.blendShapes)
            {
                int idx = faceMesh.sharedMesh.GetBlendShapeIndex(shape.shapeName);
                if (idx != -1 && !managedEmotionIndices.Contains(idx))
                {
                    managedEmotionIndices.Add(idx);
                }
            }
        }

        // --- Lip Sync Hazırlığı ---
        foreach (string vName in visemeBlendShapes)
        {
            int vIdx = faceMesh.sharedMesh.GetBlendShapeIndex(vName);
            if (vIdx != -1) visemeIndices.Add(vIdx);
        }

        isFaceSystemActive = true;
        
        if (blinkIndex != -1)
            StartCoroutine(AutoBlinkRoutine());

        if (visemeIndices.Count > 0)
            StartCoroutine(LipSyncRoutine());
            
        SetEmotion(BotEmotion.Neutral);
    }

    public void SetEmotion(BotEmotion newEmotion)
    {
        if (!isFaceSystemActive || faceMesh == null) return;
        currentEmotion = newEmotion;

        if (currentEmotionRoutine != null) StopCoroutine(currentEmotionRoutine);
        currentEmotionRoutine = StartCoroutine(TransitionToEmotion(newEmotion));
    }

    public void SetTalking(bool talkingStatus)
    {
        isTalking = talkingStatus;
    }

    [ContextMenu("Test - Mutlu (Happy)")]
    public void TestHappy() { SetEmotion(BotEmotion.Happy); }

    [ContextMenu("Test - Sinirli (Angry)")]
    public void TestAngry() { SetEmotion(BotEmotion.Angry); }
    
    [ContextMenu("Test - Normal (Neutral)")]
    public void TestNeutral() { SetEmotion(BotEmotion.Neutral); }

    [ContextMenu("Test - Konuşma Başlat/Bitir")]
    public void TestToggleTalking() 
    { 
        isTalking = !isTalking; 
        Debug.Log("Konuşma durumu: " + isTalking);
    }

    private IEnumerator TransitionToEmotion(BotEmotion targetEmotion)
    {
        EmotionProfile targetProfile = default;
        bool profileFound = false;

        foreach (var profile in emotionProfiles)
        {
            if (profile.emotion == targetEmotion)
            {
                targetProfile = profile;
                profileFound = true;
                break;
            }
        }

        // Geliştirme 1: Dictionary yerine Dizi (Array) kullanımı.
        // Sürekli yeni obje yaratmadığı için çöp toplayıcıyı (Garbage Collector) yormaz, performansı artırır.
        float[] startWeights = new float[managedEmotionIndices.Count];
        float[] targetWeights = new float[managedEmotionIndices.Count];

        for (int i = 0; i < managedEmotionIndices.Count; i++)
        {
            startWeights[i] = faceMesh.GetBlendShapeWeight(managedEmotionIndices[i]);
            targetWeights[i] = 0f;
        }

        if (profileFound && targetProfile.blendShapes != null)
        {
            foreach (var shape in targetProfile.blendShapes)
            {
                int idx = faceMesh.sharedMesh.GetBlendShapeIndex(shape.shapeName);
                if (idx != -1)
                {
                    int arrayIndex = managedEmotionIndices.IndexOf(idx);
                    if (arrayIndex != -1) targetWeights[arrayIndex] = shape.weight;
                }
            }
        }

        float t = 0;
        while (t < emotionTransitionDuration)
        {
            t += Time.deltaTime;
            float normalizedTime = t / emotionTransitionDuration;

            for (int i = 0; i < managedEmotionIndices.Count; i++)
            {
                float currentW = Mathf.Lerp(startWeights[i], targetWeights[i], normalizedTime);
                faceMesh.SetBlendShapeWeight(managedEmotionIndices[i], currentW);
            }
            yield return null;
        }

        for (int i = 0; i < managedEmotionIndices.Count; i++)
        {
            faceMesh.SetBlendShapeWeight(managedEmotionIndices[i], targetWeights[i]);
        }
    }

    private IEnumerator LipSyncRoutine()
    {
        while (isFaceSystemActive)
        {
            bool shouldTalk = isTalking;
            float volumeIntensity = 1f;

            if (!shouldTalk && voiceSource != null && voiceSource.isPlaying)
            {
                voiceSource.GetOutputData(audioSampleBuffer, 0);
                float currentVolume = 0f;
                for (int i = 0; i < audioSampleBuffer.Length; i++)
                {
                    currentVolume += Mathf.Abs(audioSampleBuffer[i]);
                }
                currentVolume /= audioSampleBuffer.Length;

                if (currentVolume > voiceVolumeThreshold)
                {
                    shouldTalk = true;
                    // Geliştirme 2: Ses şiddetine göre ağız açıklığı
                    // Fısıldarken ağzı az, bağırırken çok açılır.
                    volumeIntensity = Mathf.Clamp01(currentVolume * 15f); 
                }
            }

            if (shouldTalk)
            {
                int nextViseme = visemeIndices[Random.Range(0, visemeIndices.Count)];
                
                // Ağzın açıklığını (targetWeight) ses şiddeti (volumeIntensity) ile çarparız
                float targetWeight = Random.Range(50f, 100f) * volumeIntensity;

                if (currentVisemeIndex != -1 && currentVisemeIndex != nextViseme)
                {
                    StartCoroutine(AnimateBlendshape(currentVisemeIndex, faceMesh.GetBlendShapeWeight(currentVisemeIndex), 0f, lipSyncSpeed));
                }

                currentVisemeIndex = nextViseme;
                yield return StartCoroutine(AnimateBlendshape(currentVisemeIndex, 0f, targetWeight, lipSyncSpeed));
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }
            else
            {
                if (currentVisemeIndex != -1)
                {
                    StartCoroutine(AnimateBlendshape(currentVisemeIndex, faceMesh.GetBlendShapeWeight(currentVisemeIndex), 0f, lipSyncSpeed * 1.5f));
                    currentVisemeIndex = -1;
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    // Geliştirme 3: Göz kırpma animasyonu (SingleBlink) modüler hale getirildi
    private IEnumerator AutoBlinkRoutine()
    {
        while (isFaceSystemActive)
        {
            float waitTime = Random.Range(minBlinkWait, maxBlinkWait);
            yield return new WaitForSeconds(waitTime);

            bool doubleBlink = Random.value < 0.2f;

            yield return StartCoroutine(SingleBlink(blinkSpeed));

            if (doubleBlink)
            {
                yield return new WaitForSeconds(0.02f); // İki kırpma arası ufak bekleme
                yield return StartCoroutine(SingleBlink(blinkSpeed * 0.8f)); // İkinci kırpma daha hızlı
            }
        }
    }

    private IEnumerator SingleBlink(float speed)
    {
        // Gözü Kapat -> Bekle -> Gözü Aç
        yield return StartCoroutine(AnimateBlendshape(blinkIndex, 0f, 100f, speed));
        yield return new WaitForSeconds(0.05f);
        yield return StartCoroutine(AnimateBlendshape(blinkIndex, 100f, 0f, speed));
    }

    private IEnumerator AnimateBlendshape(int index, float startWeight, float targetWeight, float duration)
    {
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            float weight = Mathf.Lerp(startWeight, targetWeight, t / duration);
            faceMesh.SetBlendShapeWeight(index, weight);
            yield return null;
        }
        faceMesh.SetBlendShapeWeight(index, targetWeight);
    }
}
