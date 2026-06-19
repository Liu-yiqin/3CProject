using UnityEngine;

/// <summary>
/// 角色朝向控制器。
/// 
/// 它只负责让角色朝某个世界方向旋转，
/// 不负责读取输入，
/// 不负责移动角色，
/// 不负责处理重力和跳跃。
/// </summary>
public class CharacterRotator : MonoBehaviour
{
    [Header("References")]

    /// <summary>
    /// 需要被旋转的根节点。
    /// 
    /// 如果你希望整个 Player 物体旋转，
    /// 就把 Player 自己拖进来。
    /// 
    /// 如果你只希望模型旋转，
    /// 就把 Model 子物体拖进来。
    /// 
    /// 当前第 4 步推荐先拖 Player 自己。
    /// </summary>
    [SerializeField]
    private Transform rotationRoot;

    [Header("Rotate Settings")]

    /// <summary>
    /// 角色旋转速度。
    /// 
    /// 单位：度 / 秒。
    /// 
    /// 数值越大，角色转身越快。
    /// 例如：
    /// 720 表示角色最多每秒旋转 720 度。
    /// </summary>
    [SerializeField]
    private float rotateSpeed = 720f;

    /// <summary>
    /// 最小有效方向长度平方。
    /// 
    /// 当传入方向太小时，不执行旋转。
    /// 这样可以避免没有移动输入时，LookRotation 收到零向量报错。
    /// </summary>
    [SerializeField]
    private float minDirectionSqrMagnitude = 0.0001f;

    private void Awake()
    {
        ValidateReferences();
        ValidateSettings();
    }

    /// <summary>
    /// 校验引用是否正确。
    /// 
    /// 这里不做静默兜底。
    /// 如果 rotationRoot 没有配置，就直接报错并禁用脚本。
    /// </summary>
    private void ValidateReferences()
    {
        if (rotationRoot == null)
        {
            Debug.LogError($"{nameof(CharacterRotator)} 缺少 rotationRoot，请在 Inspector 中指定要旋转的节点。", this);
            enabled = false;
        }
    }

    /// <summary>
    /// 校验旋转参数是否正确。
    /// 
    /// 参数错误时直接报错，
    /// 方便尽早发现配置问题。
    /// </summary>
    private void ValidateSettings()
    {
        if (rotateSpeed <= 0f)
        {
            Debug.LogError($"{nameof(CharacterRotator)} 的 rotateSpeed 必须大于 0。", this);
            enabled = false;
        }

        if (minDirectionSqrMagnitude <= 0f)
        {
            Debug.LogError($"{nameof(CharacterRotator)} 的 minDirectionSqrMagnitude 必须大于 0。", this);
            enabled = false;
        }
    }

    /// <summary>
    /// 朝指定世界方向旋转。
    /// 
    /// 这个方法不会瞬间把角色转过去，
    /// 而是根据 rotateSpeed 平滑转向。
    /// </summary>
    /// <param name="worldDirection">希望角色面向的世界方向。</param>
    public void RotateTowards(Vector3 worldDirection)
    {
        if (rotationRoot == null)
        {
            Debug.LogError($"{nameof(CharacterRotator)} 缺少 rotationRoot，无法旋转。", this);
            return;
        }

        // 旋转只看水平面方向。
        // 不使用 y，避免角色因为跳跃、下落而抬头或低头。
        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude < minDirectionSqrMagnitude)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(worldDirection.normalized, Vector3.up);

        rotationRoot.rotation = Quaternion.RotateTowards(
            rotationRoot.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }
}