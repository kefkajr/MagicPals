using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum ObjectiveType {
    BlockExit,
    ProtectSelf
}

public class Objective : MonoBehaviour {
    public List<ObjectiveType> types;

    public Objective(ObjectiveType[] types) {
        this.types = types.ToList();
    }
}