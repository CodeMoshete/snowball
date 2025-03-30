using UnityEngine;

public class PlayerEntityAnimator : MonoBehaviour
{
    public Animator Animator;

    public void SetMoveForward(bool isMovingFwd)
    {
        if (Animator != null)
        {
            Animator.SetBool("Forward", isMovingFwd);
        }
    }

    public void SetMoveBackward(bool isMovingBwd)
    {
        if (Animator != null)
        {
            Animator.SetBool("Backward", isMovingBwd);
        }
    }

    public void SetMoveLeft(bool isMovingLeft)
    {
        if (Animator != null)
        {
            Animator.SetBool("Left", isMovingLeft);
        }
    }

    public void SetMoveRight(bool isMovingRight)
    {
        if (Animator != null)
        {
            Animator.SetBool("Right", isMovingRight);
        }
    }

    public void Jump()
    {
        if (Animator != null)
        {
            Animator.SetTrigger("Jump");
        }
    }

    public void Throw()
    {
        if (Animator != null)
        {
            Animator.SetTrigger("Throw");
        }
    }
}
