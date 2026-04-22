using UnityEngine;

public partial class WanderBot
{
    [Header("Equipment System (Sockets)")]
    [Tooltip("Sağ elde eşyanın tutunacağı Transform (Sağ el kemiğinin altındaki boş obje)")]
    [SerializeField] private Transform rightHandSocket;
    [Tooltip("Sol elde eşyanın tutunacağı Transform (Sol el kemiğinin altındaki boş obje)")]
    [SerializeField] private Transform leftHandSocket;

    [Header("Current Equipped Items")]
    [Tooltip("Karakterin şu an sağ elinde tuttuğu eşya (Otomatik atanır)")]
    public GameObject equippedRightItem;
    [Tooltip("Karakterin şu an sol elinde tuttuğu eşya (Otomatik atanır)")]
    public GameObject equippedLeftItem;

    [Header("Test System")]
    [Tooltip("Aşağıdaki 3 noktaya tıklayıp test etmek için yerden alınacak objeyi buraya sürükleyin")]
    [SerializeField] private GameObject testItemToEquip;

    /// <summary>
    /// Bir eşyayı istenen ele sabitler. Eşyanın lokal pozisyonunu ve rotasyonunu sıfırlar.
    /// (Animasyon elini uzattığında bu fonksiyon çalıştırılmalıdır).
    /// </summary>
    /// <param name="item">Yerden alınacak obje</param>
    /// <param name="isRightHand">True ise Sağ el, False ise Sol el</param>
    public void EquipItem(GameObject item, bool isRightHand)
    {
        if (item == null) return;

        Transform targetSocket = isRightHand ? rightHandSocket : leftHandSocket;

        if (targetSocket == null)
        {
            Debug.LogWarning($"WanderBot: {(isRightHand ? "Sağ" : "Sol")} el soketi (Socket) atanmamış! Lütfen Inspector'dan el kemiğinin içindeki boş objeyi sürükleyin.");
            return;
        }

        // 1. Eğer o elde zaten bir eşya varsa, onu yere at
        DropItem(isRightHand);

        // 2. Yeni eşyayı yerden al ve eldeki soketin (kemiğin) içine at
        item.transform.SetParent(targetSocket);
        
        // 3. Eşyanın pozisyonunu ve dönüşünü tam olarak soketin içine sıfırla (Elin içine tam otursun)
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        // 4. Eşyanın yere düşmemesi için fiziğini dondur
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // (İsteğe bağlı) Karakter yürürken elindeki silah kendi vücuduna çarpmasın diye collider'ı kapat
        Collider col = item.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 5. Kayıt altına al (Karakter zekasının ne tuttuğunu bilmesi için)
        if (isRightHand)
            equippedRightItem = item;
        else
            equippedLeftItem = item;
            
        // NOT: Buraya "Eline silah aldı" diye Animator'e komut yollayabiliriz.
        // if (animator != null) animator.SetBool("IsArmed", true);
    }

    /// <summary>
    /// Eldeki eşyayı yere bırakır. Fiziğini tekrar açar.
    /// </summary>
    public void DropItem(bool isRightHand)
    {
        GameObject currentItem = isRightHand ? equippedRightItem : equippedLeftItem;

        if (currentItem != null)
        {
            // 1. Eşyayı elden kopart ve ana hiyerarşiye (dünyaya) geri at
            currentItem.transform.SetParent(null); 
            
            // 2. Fiziğini geri ver ki pat diye yere düşsün
            Rigidbody rb = currentItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            // 3. Çarpışmaları tekrar aç
            Collider col = currentItem.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }

            // 4. Eli boşalt
            if (isRightHand)
                equippedRightItem = null;
            else
                equippedLeftItem = null;
        }
    }


    // --- EDİTÖR TEST BUTONLARI ---

    [ContextMenu("Test - Test Objeyi SAĞ Ele Al")]
    public void TestEquipRight() 
    { 
        if(testItemToEquip != null) EquipItem(testItemToEquip, true); 
        else Debug.LogWarning("Test Item To Equip boş!");
    }

    [ContextMenu("Test - Test Objeyi SOL Ele Al")]
    public void TestEquipLeft() 
    { 
        if(testItemToEquip != null) EquipItem(testItemToEquip, false); 
        else Debug.LogWarning("Test Item To Equip boş!");
    }

    [ContextMenu("Test - SAĞ Eldekini Yere Bırak")]
    public void TestDropRight() { DropItem(true); }
    
    [ContextMenu("Test - SOL Eldekini Yere Bırak")]
    public void TestDropLeft() { DropItem(false); }
}
