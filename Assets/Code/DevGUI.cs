using Game.Player;
using System.Text;
using UnityEngine;

public class DevGUI : MonoBehaviour
{
    public PlayerController playerController;

    private string debugInfo = "";

    void Update()
    {
        debugInfo = BuildDebugInfo();
    }

    public string BuildDebugInfo()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Position: {transform.position}");
        sb.AppendLine($"Stamina: {playerController.currentStamina}");
        sb.AppendLine($"Velocity: {playerController.velocity}");

        return sb.ToString();
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 500, 200), debugInfo);
    }
}
