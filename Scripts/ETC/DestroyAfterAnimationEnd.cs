using UnityEngine;

public class DestroyAfterAnimationEnd : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (_animator != null)
        {
            float animationLength = _animator.GetCurrentAnimatorStateInfo(0).length;
            Destroy(gameObject, animationLength);
        }
        else
        {
            Destroy(gameObject, 1f); 
        }
    }
}
