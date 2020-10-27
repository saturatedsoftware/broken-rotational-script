using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using activators;
using puzzles;

public class TEST_ROTATION_BULLSHIT_DELETE_THIS : Puzzle_Interface_Targetable_Base_Class, Ipuzzle
{
    [HideInInspector] public Puzzle puzzleTarget;

    [HideInInspector] public bool isSolved;

    [HideInInspector] public bool hasPlayerInteractedWith;  //this is needed for thee fallible puzzle system.

    public List<Activator_targetable_Base_class> activatorsToActivateSetvalue = new List<Activator_targetable_Base_class>();
    public List<Iactivator> activatorsToActivate = new List<Iactivator>();

    [Header("rotate settings")]

    [HideInInspector] public Quaternion defaultLocalTransformRotation;
    [HideInInspector] public Vector3 defaultLocalUp;

    bool needsXSolution;
    bool needsYSolution;
    bool needsZSolution;
    bool needsXYZSolution;

    bool needsXRotationConstraint;
    bool needsYRotationConstraint;
    bool needsZRotation;

    float Xrotation = 0;
    float Yrotation = 0;
    float Zrotation = 0;

    float rotateSpeed = 100f;


    Quaternion newSnapRotation;
    float snapLerpSpeed = 5f;

    bool needsXSnap;
    bool needsYSnap;
    bool needsZSnap;

    float XSnapRotation;
    float YSnapRotation;
    float ZSnapRotation;


    public float rotationSnapDegrees = 90f;
    float offsetAngle;
    Quaternion actualRotation;

    Vector3 vec;

    public Rotation_Puzzle_Solve_Target rotationSolveTarget;

    float rotationTargetThreshold;

    float previousAngle;
    float angle;
    Vector3 mouse_pos;
    Vector3 object_pos;
    bool isMouseUp;

    public enum rotationType { Horizontal, Vertical, Dial, Trackball }
    public rotationType currentRotationType;

    GameObject reparentTarget;  //fixes issue where puzzle elements do not rotate when their parents are moved!!!

    public bool needsRotationClamp;
    public float clampMin;
    public float clampMax;

    Vector2 mouseLook;    //testing!!!
    Vector2 smoothV;    //testing!!!
    float sensitivity = 2.0f;   //testing!!!
    float smoothing = 2.0f; //testing!!!

    Vector3 lastForward;
    float offsetRotation;

    GameObject empty;
    void OnEnable()
    {

        TransformParentSpawn();

        defaultLocalTransformRotation = gameObject.transform.localRotation;

        foreach (Activator_targetable_Base_class activator in activatorsToActivateSetvalue)
        {
            if (activator is Iactivator)
            {
                activatorsToActivate.Add(activator as Iactivator);
            }
        }


        switch (currentRotationType)
        {
            case rotationType.Horizontal:
                needsYSolution = true;
                needsXRotationConstraint = true;
                needsYSnap = true;


                break;

            case rotationType.Vertical:
                needsXSolution = true;
                needsYRotationConstraint = true;
                needsXSnap = true;


                break;

            case rotationType.Dial:
                needsZSolution = true;
                needsZRotation = true;
                needsZSnap = true;


                break;

            case rotationType.Trackball:
                needsXYZSolution = true;
                needsXSnap = true;
                needsYSnap = true;
                needsZSnap = true;


                break;
            default:

                break;
        }
    }


    void Update()
    {
        if (isMouseUp)
        {
            gameObject.transform.localRotation = Quaternion.Lerp(gameObject.transform.localRotation, newSnapRotation, Time.deltaTime * snapLerpSpeed);

            //offset  = saved last forward  - current position
            var angle = Vector3.Angle(lastForward, transform.forward);
            var cross = Vector3.Cross(lastForward, transform.forward);
            if (cross.y < 0) angle = -angle;

            Debug.Log(lastForward + "last forward");

            Debug.Log(angle + "angle");

            offsetRotation = angle;
        }
    }

    void OnMouseDown()
    {
        previousAngle = transform.localEulerAngles.z;
        offsetAngle = CalculateAngleForDialRotation();
    }

    void OnMouseDrag()
    {
        isMouseUp = false;


        if (HUD_manager.CurrentMenuState == HUD_manager.MenuState.PuzzleMenu)   //WAS   gameController_Master.isPuzzleMenuActive
        {
            hasPlayerInteractedWith = true;

            //SetRotationConstraints();

            NewSetRotationConstraints();    //testing!!!

            if (currentRotationType == rotationType.Vertical)
            {
                transform.localRotation = Quaternion.AngleAxis(mouseLook.y, Vector3.right);    //up/down
            }
            if (currentRotationType == rotationType.Horizontal)
            {
                transform.localRotation = Quaternion.AngleAxis(mouseLook.x, Vector3.up);    //right/left
            }

            else if (needsZRotation)
            {
                var newAngle = CalculateAngleForDialRotation();
                transform.localRotation = Quaternion.Euler(new Vector3(0, 0, -newAngle + offsetAngle + previousAngle));  //spinning dial style rotation
            }
            else if (needsXYZSolution)
            {
                transform.Rotate(Game_Manager.mainCamera.transform.TransformVector(Xrotation, Yrotation, Zrotation), Space.World);  //trackball style rotation
            }
        }
    }

    void OnMouseUp()
    {
        isMouseUp = true;

        CheckForAndSetSnappingForRotation();

        CheckForRotationPuzzleSolution();

        lastForward = transform.forward;
    }

    void TransformParentSpawn() //this bit fixes issues with facing/zero rotation!!!
    {
        empty = new GameObject("rotation puzzle interface nest");
        empty.transform.SetPositionAndRotation(transform.position, transform.rotation);

        if (transform.parent != null)
        {
            reparentTarget = transform.parent.gameObject;    //fixes issue where puzzle elements do not rotate when their parents are moved!!!
        }

        transform.parent = empty.transform;

        transform.localRotation = Quaternion.identity;

        if (transform.parent != null)
        {
            empty.transform.parent = reparentTarget.transform;      //fixes issue where puzzle elements do not rotate when their parents are moved!!!
        }
    }


    void SetRotationConstraints()
    {
        if (needsXRotationConstraint)
        {
            Xrotation = defaultLocalTransformRotation.x;
        }
        else if (!needsZRotation)
        {
            Xrotation = -(Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime);
        }

        if (needsYRotationConstraint)
        {
            Yrotation = defaultLocalTransformRotation.y;
        }
        else if (!needsZRotation)
        {
            Yrotation = -(Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime);
        }
    }

    void NewSetRotationConstraints() 
    {

        var md = new Vector2(-Input.GetAxisRaw("Mouse X"), -Input.GetAxisRaw("Mouse Y"));

        md = Vector2.Scale(md, new Vector2(sensitivity * smoothing, sensitivity * smoothing));
        smoothV.x = Mathf.Lerp(smoothV.x, md.x, 1f / smoothing);
        smoothV.y = Mathf.Lerp(smoothV.y, md.y, 1f / smoothing);
        mouseLook += smoothV;

        if (needsRotationClamp)
        {
            if (needsYRotationConstraint)
            {
                mouseLook.y = Mathf.Clamp(mouseLook.y, clampMin, clampMax);
            }
            if (needsXRotationConstraint)
            {
                mouseLook.x = Mathf.Clamp(mouseLook.x, clampMin, clampMax);
            }
        }
    }


    void CheckForAndSetSnappingForRotation()
    {
        vec = transform.localEulerAngles;

        if (needsXSnap)
        {
            vec.x = Mathf.Round(vec.x / rotationSnapDegrees) * rotationSnapDegrees;
        }

        if (needsYSnap)
        {
            vec.y = Mathf.Round(vec.y / rotationSnapDegrees) * rotationSnapDegrees;
        }

        if (needsZSnap)
        {
            vec.z = Mathf.Round(vec.z / rotationSnapDegrees) * rotationSnapDegrees;
        }

        newSnapRotation = Quaternion.Euler(vec);
    }

    public void CheckForRotationPuzzleSolution()
    {
        if (needsXSolution)
        {
            rotationTargetThreshold = rotationSnapDegrees / 2;

            float difference = Vector3.Angle(transform.up, rotationSolveTarget.transform.up);    //this new method works for vertical puzzles!!! reccomend converting all methods over to this!!!

            Debug.Log("the difference in angle for " + this.gameObject + "is " + difference);

            if (difference <= rotationTargetThreshold)
            {

                foreach (Iactivator activator in activatorsToActivate)
                {
                    activator.ActivateActivators();
                }

                isSolved = true;
                Debug.Log("solved");

                //puzzleTarget.CheckIfPuzzleIsSolved();


                return;
            }
            else
            {
                isSolved = false;
                Debug.Log("not solved");

                //puzzleTarget.CheckIfPuzzleIsSolved();   //testing  this for theee on fail in the desert oasis!!!

            }
        }

        if (needsYSolution)
        {
            rotationTargetThreshold = rotationSnapDegrees / 2;

            float difference = Vector3.Angle(transform.right, rotationSolveTarget.transform.right);    //this new method works for vertical puzzles!!! reccomend converting all methods over to this!!!

            //Debug.Log("the difference in angle for " + this.gameObject + "is " + difference);

            if (difference <= rotationTargetThreshold)
            {

                foreach (Iactivator activator in activatorsToActivate)
                {
                    activator.ActivateActivators();
                }

                isSolved = true;
                Debug.Log("solved");

                //puzzleTarget.CheckIfPuzzleIsSolved();

                return;
            }
            else
            {
                isSolved = false;
                Debug.Log("not solved");

                //puzzleTarget.CheckIfPuzzleIsSolved();   //testing  this for theee on fail in the desert oasis!!!

            }
        }

        if (needsZSolution)
        {
            rotationTargetThreshold = rotationSnapDegrees / 2;

            float difference = Vector3.Angle(transform.right, rotationSolveTarget.transform.right);    //this new method works for vertical puzzles!!! reccomend converting all methods over to this!!!

            Debug.Log("the difference in angle for " + this.gameObject + "is " + difference);

            if (difference <= rotationTargetThreshold)
            {
                foreach (Iactivator activator in activatorsToActivate)
                {
                    activator.ActivateActivators();
                }

                isSolved = true;
                Debug.Log("solved");


                puzzleTarget.CheckIfPuzzleIsSolved();

                return;
            }
            else
            {
                isSolved = false;
                Debug.Log("not solved");

                puzzleTarget.CheckIfPuzzleIsSolved();   //testing  this for theee on fail in the desert oasis!!!
            }
        }

        if (needsXYZSolution)
        {
            rotationTargetThreshold = rotationSnapDegrees / 2;

            float difference = Vector3.Angle(transform.forward, rotationSolveTarget.transform.forward);

            Debug.Log("the difference in angle for " + this.gameObject + "is " + difference);


            if (difference <= rotationTargetThreshold)
            {
                foreach (Iactivator activator in activatorsToActivate)
                {
                    activator.ActivateActivators();
                }

                isSolved = true;
                Debug.Log("solved");


                puzzleTarget.CheckIfPuzzleIsSolved();

                return;
            }
            else
            {
                isSolved = false;
                Debug.Log("not solved");

                puzzleTarget.CheckIfPuzzleIsSolved();   //testing  this for theee on fail in the desert oasis!!!
            }
        }
    }

    float CalculateAngleForDialRotation()
    {
        mouse_pos = Input.mousePosition;
        mouse_pos.z = 0f; //The distance between the camera and object
        object_pos = Game_Manager.mainCamera.WorldToScreenPoint(gameObject.transform.position);

        mouse_pos.x = mouse_pos.x - object_pos.x;
        mouse_pos.y = mouse_pos.y - object_pos.y;

        angle = Mathf.Atan2(mouse_pos.y, mouse_pos.x) * Mathf.Rad2Deg;

        return angle;
    }

    bool Ipuzzle.isSolved()
    {
        return isSolved;
    }

    void Ipuzzle.setPuzzleTarget(Puzzle target)
    {
        puzzleTarget = target;
    }

    public bool hasPlayerInteracted()
    {

        return hasPlayerInteractedWith;

    }

    public void ResetInteractivity()
    {
        Debug.LogError(gameObject.name + "is reseting!!!");

        hasPlayerInteractedWith = false;
        isSolved = false;

        newSnapRotation = defaultLocalTransformRotation;
    }
}

