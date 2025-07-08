using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Equipment Database", menuName = "October/Equipment Database")]
public class EquipmentDatabase : ScriptableObject
{
    [SerializeField] protected EquipmentModel[] hatEquipments;
    [SerializeField] protected EquipmentModel[] armorEquipments;
    [SerializeField] protected EquipmentModel[] ringEquipments;
    [SerializeField] protected EquipmentModel[] necklaceEquipments;
    [SerializeField] protected EquipmentModel[] beltEquipments;
    [SerializeField] protected EquipmentModel[] shoeEquipments;

    public EquipmentModel[] GetEquipmentsByType(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.Hat: return hatEquipments;
            case EquipmentType.Armor: return armorEquipments;
            case EquipmentType.Ring: return ringEquipments;
            case EquipmentType.Necklace: return necklaceEquipments;
            case EquipmentType.Belt: return beltEquipments;
            case EquipmentType.Shoes: return shoeEquipments;
            default: return Array.Empty<EquipmentModel>();
        }
    }

    public EquipmentModel GetEquipmentById(EquipmentType type, int id)
    {
        var equipments = GetEquipmentsByType(type);
        if (id >= 0 && id < equipments.Length)
            return equipments[id];
        return null;
    }
}