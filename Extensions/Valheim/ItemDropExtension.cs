namespace Extensions.Valheim;

public static class ItemDropExtension
{
    public static string LocalizeName(this ItemDrop drop) => drop.m_itemData.LocalizeName();
    public static string LocalizeName(this ItemDrop.ItemData drop) => drop.m_shared.LocalizeName();
    public static string LocalizeName(this ItemDrop.ItemData.SharedData drop) => drop.m_name.Localize();
}