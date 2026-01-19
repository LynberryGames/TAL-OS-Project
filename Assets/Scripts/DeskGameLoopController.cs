using UnityEngine;
using UnityEngine.Playables;
using TMPro;


public class DeskGameLoopController : MonoBehaviour
{

    public AudioController audioController;

    private enum State
    {
        PlayingEnter,
        AwaitingDecision,
        PlayingSuccess,
        PlayingFail
    }

    [Header("Timelines")]
    [SerializeField] private PlayableDirector enterDirector;
    [SerializeField] private PlayableDirector successDirector;
    [SerializeField] private PlayableDirector failDirector;

    [Header("Alembic Roots (toggle visibility)")]
    [SerializeField] private GameObject enterRoot;
    [SerializeField] private GameObject successRoot;
    [SerializeField] private GameObject failRoot;

    [Header("ID Spawning + Eject")]
    [SerializeField] private GameObject idCardPrefab;
    [SerializeField] private Transform idMouthPoint;

    [SerializeField] private float ejectForwardImpulse = 1.5f;
    [SerializeField] private float ejectUpImpulse = 0.3f;
    [SerializeField] private float ejectSpinTorque = 0.0f;


    [Header("Scoring")]
    [SerializeField] private TMP_Text correctText;
    [SerializeField] private TMP_Text mistakesText;

    private int correct = 0;
    private int mistakes = 0;

    private bool currentIdIsValid = true;


    private State state;

    private void OnEnable()
    {
        if (enterDirector != null) enterDirector.stopped += OnEnterStopped;
        if (successDirector != null) successDirector.stopped += OnSuccessStopped;
        if (failDirector != null) failDirector.stopped += OnFailStopped;
    }

    private void OnDisable()
    {
        if (enterDirector != null) enterDirector.stopped -= OnEnterStopped;
        if (successDirector != null) successDirector.stopped -= OnSuccessStopped;
        if (failDirector != null) failDirector.stopped -= OnFailStopped;
    }

    private void Start()
    {
        UpdateScoreUI();

        StartRound();
    }

    private void StartRound()
    {
        // Temporarily ignore stopped events while stopping/resetting
        state = State.AwaitingDecision;

        audioController.PlayMachineSound();

        SetActiveRoot(enter: true, success: false, fail: false);
        StopAllDirectors();

        if (enterDirector == null) return;

        enterDirector.time = 0;
        enterDirector.Play();
        state = State.PlayingEnter;
    }

    public void Accept()
    {
        if (state != State.AwaitingDecision) return;

        // Green means "valid"
        
        if (currentIdIsValid) AddCorrect();   // accepted a valid ID
        else AddMistake();                    // accepted an invalid ID


        state = State.AwaitingDecision;

        SetActiveRoot(enter: false, success: true, fail: false);
        StopAllDirectors();

        if (successDirector == null) return;

        successDirector.time = 0;
        successDirector.Play();
        state = State.PlayingSuccess;
    }


    public void Reject()
    {
        if (state != State.AwaitingDecision) return;
        
        if (currentIdIsValid) AddMistake();   // you rejected a valid ID
        else AddCorrect();                    // you rejected an invalid/expired ID (correct)
        audioController.PlayRobotDie();


        state = State.AwaitingDecision;

        SetActiveRoot(enter: false, success: false, fail: true);
        StopAllDirectors();

        if (failDirector == null) return;

        failDirector.time = 0;
        failDirector.Play();
        state = State.PlayingFail;
    }


    private void OnEnterStopped(PlayableDirector d)
    {
        if (state != State.PlayingEnter) return;

        // Keep head visible while deciding
        SetActiveRoot(enter: true, success: false, fail: false);

        SpawnAndEjectID();

        state = State.AwaitingDecision;
    }

    private void OnSuccessStopped(PlayableDirector d)
    {
        if (state != State.PlayingSuccess) return;
        StartRound();
    }

    private void OnFailStopped(PlayableDirector d)
    {
        if (state != State.PlayingFail) return;
        StartRound();
    }

    private void SpawnAndEjectID()
    {
        if (idCardPrefab == null || idMouthPoint == null) return;

        GameObject id = Instantiate(idCardPrefab, idMouthPoint.position, idMouthPoint.rotation);

        var result = id.GetComponentInChildren<IDCardResult>(true);
        currentIdIsValid = (result != null) ? result.isValid : true;



        Rigidbody rb = id.GetComponentInChildren<Rigidbody>();
        if (rb == null) return;

        rb.WakeUp();

        audioController.PlayEject();
        // Shoot out in the direction the mouth point is facing
        Vector3 forward = idMouthPoint.forward;

        rb.AddForce(forward * ejectForwardImpulse, ForceMode.Impulse);
        rb.AddForce(Vector3.up * ejectUpImpulse, ForceMode.Impulse);

        if (ejectSpinTorque != 0f)
            rb.AddTorque(Vector3.up * ejectSpinTorque, ForceMode.Impulse);
    }


    private void StopAllDirectors()
    {
        if (enterDirector != null) enterDirector.Stop();
        if (successDirector != null) successDirector.Stop();
        if (failDirector != null) failDirector.Stop();
    }

    private void SetActiveRoot(bool enter, bool success, bool fail)
    {
        if (enterRoot != null) enterRoot.SetActive(enter);
        if (successRoot != null) successRoot.SetActive(success);
        if (failRoot != null) failRoot.SetActive(fail);
    }

    private void AddCorrect()
    {
        correct++;
        UpdateScoreUI();
    }

    private void AddMistake()
    {
        mistakes++;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (correctText != null) correctText.text = correct.ToString();
        if (mistakesText != null) mistakesText.text = mistakes.ToString();
    }

}
