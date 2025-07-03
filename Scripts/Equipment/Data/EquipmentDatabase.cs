using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Equipment Database", menuName = "October/Equipment Database")]
public class EquipmentDatabase : ScriptableObject
{
    [SerializeField] protected EquipmentData[] hatEquipments;
    [SerializeField] protected EquipmentData[] armorEquipments;
    [SerializeField] protected EquipmentData[] ringEquipments;
    [SerializeField] protected EquipmentData[] necklaceEquipments;
    [SerializeField] protected EquipmentData[] beltEquipments;
    [SerializeField] protected EquipmentData[] shoeEquipments;

    public EquipmentData[] GetEquipmentsByType(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.Hat: return hatEquipments;
            case EquipmentType.Armor: return armorEquipments;
            case EquipmentType.Ring: return ringEquipments;
            case EquipmentType.Necklace: return necklaceEquipments;
            case EquipmentType.Belt: return beltEquipments;
            case EquipmentType.Shoes: return shoeEquipments;
            default: return Array.Empty<EquipmentData>();
        }
    }

    public EquipmentData GetEquipmentById(EquipmentType type, int id)
    {
        var equipments = GetEquipmentsByType(type);
        if (id >= 0 && id < equipments.Length)
            return equipments[id];
        return null;
    }
}