namespace KSpider.Admin.Tasks;

/// <summary>
///     每次派发的唯一 token；节点与 admin 的状态写入都必须带 token 条件。
/// </summary>
/// <remarks>
///     token 解决“旧节点迟到终态覆盖新节点进度”的竞态：重派生成新 token 后，
///     旧 token 的条件更新会返回 0 行，从而被安全忽略。
/// </remarks>
/// <param name="Value">token 文本。</param>
public readonly record struct DispatchToken(string Value)
{
    /// <summary>
    ///     生成新的派发 token。
    /// </summary>
    public static DispatchToken New()
    {
        return new DispatchToken(Guid.NewGuid().ToString("N"));
    }

    /// <summary>
    ///     从已有文本构造 token。
    /// </summary>
    /// <param name="value">token 文本。</param>
    /// <exception cref="ArgumentException">文本为空。</exception>
    public static DispatchToken From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("dispatch token 不能为空", nameof(value));
        }

        return new DispatchToken(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }
}
