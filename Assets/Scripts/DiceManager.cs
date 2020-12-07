using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DiceManager : MonoBehaviour {

    [SerializeField]
    GameObject plane;

    [Range(0.1f, 2f)]
    [SerializeField]
    float checkSens = 0.01f;

    [SerializeField]
    TMP_Text text;


    [SerializeField]
    GameObject[] diceObjects;

    Die[] dice;

    Dictionary<Die, Die> simulDice = new Dictionary<Die, Die>();

    PhysicsScene physScene;
    Scene simulationScene;
    PhysicsScene simulationPhysScene;

    [HideInInspector]
    public static int[] dieStats = new int[6];

    private bool continiousThrow = false;
    public bool ContiniousThrow { get => continiousThrow; }
    public void ToggleAutoThrow(bool value) {
        continiousThrow = value;
    }


    public static DiceManager instance;

    bool allResting;
    bool allFlat;

    private void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(this);
        }
    }

    // Start is called before the first frame update
    void Start() {
        Physics.autoSimulation = false;

        physScene = SceneManager.GetActiveScene().GetPhysicsScene();
        simulationScene = SceneManager.CreateScene("Simulation Scene", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        simulationPhysScene = simulationScene.GetPhysicsScene();
        SceneManager.MoveGameObjectToScene(GameObject.Instantiate(plane), simulationScene);

        dice = new Die[diceObjects.Length];

        for (int i = 0; i < diceObjects.Length; i++) {
            GameObject dieObject = diceObjects[i];
            dice[i] = new Die(dieObject, dieObject.GetComponent<Rigidbody>(), dieObject.transform.position);
            GameObject simulDieObject = Instantiate(diceObjects[i]);
            SceneManager.MoveGameObjectToScene(simulDieObject, simulationScene);
            simulDice.Add(dice[i], new Die(simulDieObject, simulDieObject.GetComponent<Rigidbody>(), simulDieObject.transform.position));
        }
    }

    private void FixedUpdate() {
        simulationPhysScene.Simulate(Time.fixedDeltaTime);
        physScene.Simulate(Time.fixedDeltaTime);
        allResting = true;
        allFlat = true;
        foreach (Die die in dice) {
            die.CheckDieCondition();
            allResting &= die.Resting;
            allFlat &= !die.Angled;
        }
        if (allResting && !allFlat) {
            foreach (Die die in dice) {
                if (die.Angled) {
                    ApplyForce(die);
                }
            }
            Simulate();
        }
        else if (continiousThrow && allResting && allFlat) {
            ThrowAllDice();
        }

    }

    public void ResetPositions() {
        foreach (Die die in dice) {
            die.gameobject.transform.position = die.startingPos;
        }
    }

    public void ThrowAllDice() {
        if(allResting && allFlat) {
            foreach (Die die in dice) {
                die.dieThrown = true;
                simulDice[die].dieThrown = true;
                ApplyForce(die);
            }

            Simulate();
        }
    }

    internal void ThrowDie(Die die) {
        die.dieThrown = true;

        ApplyForce(die);

        Simulate();
    }

    private void ApplyForce(Die die) {
        Vector3 force = (new Vector3(Random.Range(-1, 1), (Random.value + 0.1f) * 20, Random.Range(-1, 1)).normalized + (die.startingPos - die.gameobject.transform.position).normalized * 0.2f).normalized * Random.Range(8, 10);
        Vector3 angularVelocity = new Vector3(Random.Range(-200, 200), Random.Range(-200, 200), Random.Range(-200, 200)).normalized * 999999;

        die.rigidbody.AddForce(force, ForceMode.VelocityChange);
        die.rigidbody.angularVelocity = angularVelocity;

        simulDice[die].rigidbody.AddForce(force, ForceMode.VelocityChange);
        simulDice[die].rigidbody.angularVelocity = angularVelocity;
    }

    private void Simulate() {
        bool allResting;
        do {
            simulationPhysScene.Simulate(Time.fixedDeltaTime);

            allResting = true;

            foreach (Die die in simulDice.Values) {
                die.CheckDieCondition();
                allResting &= die.Resting;
            }
        } while (!allResting);
    }





    internal class Die {
        public GameObject gameobject;
        public Rigidbody rigidbody;
        public Vector3 startingPos;
        public DieResult lastResult;


        public bool dieChecked = true;
        public bool dieThrown = false;
        public bool Angled { get { return lastResult.angleFromSide > 15; } }
        public bool Resting { get { return rigidbody.angularVelocity.magnitude == 0 && rigidbody.velocity.magnitude == 0; } }

        public Die(GameObject _gameObject, Rigidbody _rigidbody, Vector3 _startingPosition) {
            gameobject = _gameObject;
            rigidbody = _rigidbody;
            startingPos = _startingPosition;
            lastResult = CheckDieSide();
        }
        public DieResult CheckDieSide() {
            float minimumAngle = float.MaxValue;
            int side = 0;
            Vector3 dieUp = gameobject.transform.InverseTransformDirection(Vector3.up);
            float[] angles = new float[6];
            for (int i = 1; i <= 6; i++) {
                Vector3 vector = SideVector3(i);
                float angle = Vector3.Angle(vector, dieUp);
                angles[i - 1] = angle;
                if (angle < minimumAngle) {
                    side = i;
                    minimumAngle = angle;
                }
            }
            lastResult = new DieResult() { dieSide = side, angleFromSide = minimumAngle };
            return lastResult;
        }

        public void CheckDieCondition() {
            if (!dieChecked && Resting) {
                CheckDieSide();
                if (!Angled) {
                    dieChecked = true;
                    Debug.Log(lastResult.dieSide);
                    dieStats[lastResult.dieSide - 1]++;
                }
            }
            else if (Mathf.Abs(rigidbody.angularVelocity.magnitude) > instance.checkSens && dieThrown) {
                dieThrown = false;
                dieChecked = false;
            }
        }

        static Vector3 SideVector3(int side) {
            switch (side) {
                case 1:
                    return Vector3.forward;
                case 2:
                    return Vector3.down;
                case 3:
                    return Vector3.left;
                case 4:
                    return Vector3.right;
                case 5:
                    return Vector3.up;
                case 6:
                    return Vector3.back;
                default:
                    Debug.LogError("None side number was entered!");
                    return Vector3.zero;
            }
        }
    }

    internal struct DieResult {
        public int dieSide;
        public float angleFromSide;

        public static DieResult InitPosition { get { return new DieResult { dieSide = 5, angleFromSide = 0 }; } }


    }


}
