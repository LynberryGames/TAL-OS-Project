using UnityEngine;

public class AudioController : MonoBehaviour
{
    public AudioSource button;
    public AudioSource eject;
    public AudioSource robotDie;
    public AudioSource machine;

    public void PlayButton() { button.Play(); }
    public void PlayEject() { eject.Play(); }
    public void PlayRobotDie() { robotDie.Play(); }
    public void PlayMachineSound() { machine.Play(); }

}
