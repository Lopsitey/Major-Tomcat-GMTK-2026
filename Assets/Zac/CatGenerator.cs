using UnityEngine;

public class CatGenerator : MonoBehaviour
{
    [Header("Cat Sprite Options")]
    [SerializeField] private Sprite[] headSprites;
    [SerializeField] private Sprite[] tailSprites;

    [Header("Border Sprite Options")]
    [SerializeField] private Sprite[] headBorderSprites;
    [SerializeField] private Sprite[] tailBorderSprites;

    [Header("Cat Parts")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private SpriteRenderer tailRenderer;

    [Header("Cat Borders")]
    [SerializeField] private SpriteRenderer headBorderRenderer;
    [SerializeField] private SpriteRenderer tailBorderRenderer;

    [Header("Colour")]
    [SerializeField] private Color[] colours;

    private void Start()
    {
        GenerateRandomCat();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateRandomCat();
        }
    }

    public void GenerateRandomCat()
    {
        int headRandom = Random.Range(0, headSprites.Length);
        int tailRandom = Random.Range(0, tailSprites.Length);

        // Choose random sprites
        headRenderer.sprite = headSprites[headRandom];
        tailRenderer.sprite = tailSprites[tailRandom];

        headBorderRenderer.sprite = headBorderSprites[headRandom];
        tailBorderRenderer.sprite = tailBorderSprites[tailRandom];

        // Choose random colour
        Color randomColour = colours[Random.Range(0, colours.Length)];
        headRenderer.color = randomColour;
        tailRenderer.color = randomColour;
        bodyRenderer.color = randomColour;


    }

}

