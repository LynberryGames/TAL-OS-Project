using System;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class IDCardGenerator : MonoBehaviour
{
    [Header("Text on the ID card")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text occupationText;
    [SerializeField] TMP_Text modelText;
    [SerializeField] TMP_Text issuedText;
    [SerializeField] TMP_Text expiryText;
    [SerializeField] TMP_Text serialText;

    [Header("Data lists")]
    [SerializeField]
    string[] firstNames =
    {
        "Ava","Milo","Noah","Iris","Juno","Ezra","Nova","Theo"
    };

    [SerializeField]
    string[] occupations =
    {
        "Technician","Clerk","Inspector","Courier","Mechanic","Guard"
    };

    // These two arrays are linked by index
    [SerializeField]
    string[] models =
    {
        "MX-11","SR-04","CLV-2","DPT-7","NX-03"
    };

    [SerializeField]
    string[] serialPrefixes =
    {
        "MX","SR","CV","DP","NX"
    };

    // Fixed in-game date
    DateTime gameToday = new DateTime(2056, 11, 17);

    private int forcedFaceIndex = -1;

    public void SetFaceIndex(int index)
    {
        forcedFaceIndex = index;
    }


    void Awake()
    {
        GenerateID();
    }



    void GenerateID()
    {

        bool madeExpired = false;
        bool madeMismatch = false;

        // ----- NAME -----
        string firstName = firstNames[Random.Range(0, firstNames.Length)];
        nameText.text = firstName;

        // ----- OCCUPATION -----
        string job = occupations[Random.Range(0, occupations.Length)];
        occupationText.text = job;

        // ----- MODEL + PREFIX -----
        int modelIndex = Random.Range(0, models.Length);
        string model = models[modelIndex];
        string prefix = serialPrefixes[modelIndex];

        modelText.text = model;

        // ----- ISSUED DATE -----
        int issuedDaysAgo = Random.Range(1, 365 * 5);
        DateTime issuedDate = gameToday.AddDays(-issuedDaysAgo);

        // ----- EXPIRY DATE -----
        int validDays = Random.Range(365, 365 * 5);
        DateTime expiryDate = issuedDate.AddDays(validDays);

        // ----- SERIAL -----
        string serial = GenerateSerial(prefix);

        // ----- FAULT: EXPIRED (15%) -----
        if (Random.value < 0.15f)
        {

            madeExpired = true;
            int expiredDaysAgo = Random.Range(1, 366);
            expiryDate = gameToday.AddDays(-expiredDaysAgo);
            issuedDate = expiryDate.AddDays(-Random.Range(30, 365 * 3));
        }

        // ----- FAULT: SERIAL MISMATCH (15%) -----
        if (Random.value < 0.15f)
        {

            madeMismatch = true;
            string wrongPrefix = prefix;

            while (wrongPrefix == prefix)
            {
                wrongPrefix = serialPrefixes[Random.Range(0, serialPrefixes.Length)];
            }

            serial = GenerateSerial(wrongPrefix);
        }

        var result = GetComponent<IDCardResult>();
        if (result != null)
            result.isValid = !(madeExpired || madeMismatch);

        // ----- APPLY TO CARD -----
        issuedText.text = FormatDate(issuedDate);
        expiryText.text = FormatDate(expiryDate);
        serialText.text = serial;
    }

    string GenerateSerial(string prefix)
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        int number = Random.Range(0, 10000);
        char a = letters[Random.Range(0, letters.Length)];
        char b = letters[Random.Range(0, letters.Length)];

        return prefix + "-" + number.ToString("0000") + "-" + a + b;
    }

    string FormatDate(DateTime date)
    {
        return date.ToString("dd MMM yyyy").ToUpperInvariant();
    }
}
