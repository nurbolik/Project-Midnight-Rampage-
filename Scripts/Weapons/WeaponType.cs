using System;
using UnityEngine;

[Serializable]
public enum WeaponType
{
    [InspectorName("Fist")] NoWeapon = 0,
    [InspectorName("Knife")] Knife = 1,
    [InspectorName("Katana")] Katana = 2,
    [InspectorName("Bat")] Ballbat = 3,
    [InspectorName("Pistol")] Pistol = 4,
    [InspectorName("Uzi")] Uzi = 5,
    [InspectorName("Rifle")] Rifle = 6,
    [InspectorName("Shotgun")] Shotgun = 7,
}
