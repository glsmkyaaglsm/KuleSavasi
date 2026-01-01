using UnityEngine;

public class AirParallax : MonoBehaviour
{
    [Tooltip("Bulutun kayma hýzý. Arka bulutlar için küçük (0.2), öncekiler için büyük (0.8) deðerler ver.")]
    public float moveSpeed = 0.5f;

    // Bulutun ekranýn dýþýna çýktýðý ve yeniden doðacaðý sýnýr noktalarý
    // Kendi ekran geniþliðine göre bu deðerleri Unity içinden test ederek deðiþtirebilirsin.
    public float leftBoundary = -15f;
    public float rightSpawnPoint = 15f;

    void Update()
    {
        // Bulutu sürekli sola kaydýr
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

        // Eðer belirlenen sol sýnýrdan çýktýysa, sað tarafa ýþýnla
        if (transform.position.x < leftBoundary)
        {
            Vector3 newPos = transform.position;
            newPos.x = rightSpawnPoint;
            // Bulutlarýn hep ayný hizada doðmamasý için ufak bir Y varyasyonu eklenebilir
            newPos.y += Random.Range(-0.5f, 0.5f);
            transform.position = newPos;
        }
    }
}