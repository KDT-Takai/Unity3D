using UnityEngine;
using UnityEngine.UI;

// ロボット背中のバッテリーと画面のバッテリー表示。回収数だけ目盛りが点灯し、満タンでアンテナが光る
public class EnergyGauge : MonoBehaviour
{
    public GameManager gameManager;
    public Renderer[] segments;
    public Renderer antennaTip;
    public Material offMaterial;
    public Material onMaterial;
    public Image[] uiCells;
    public Color uiOffColor = new Color(0.2f, 0.2f, 0.22f);
    public Color uiOnColor = new Color(1f, 0.85f, 0.1f);

    void Update()
    {
        int count = gameManager.collectedCount;
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].sharedMaterial = i < count ? onMaterial : offMaterial;
        }
        for (int i = 0; i < uiCells.Length; i++)
        {
            uiCells[i].color = i < count ? uiOnColor : uiOffColor;
        }
        antennaTip.sharedMaterial = count >= segments.Length ? onMaterial : offMaterial;
    }
}
