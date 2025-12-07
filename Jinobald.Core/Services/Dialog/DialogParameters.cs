using System.Collections;
using System.Collections.Generic;

namespace Jinobald.Core.Services.Dialog;

/// <summary>
///     다이얼로그 파라미터 구현
/// </summary>
public class DialogParameters : IDialogParameters, IEnumerable
{
    private readonly Dictionary<string, object> _parameters = new();

    public DialogParameters()
    {
    }

    public DialogParameters(params (string Key, object Value)[] parameters)
    {
        foreach (var (key, value) in parameters)
        {
            Add(key, value);
        }
    }

    public void Add(string key, object value)
    {
        _parameters[key] = value;
    }

    public T? GetValue<T>(string key)
    {
        if (_parameters.TryGetValue(key, out var value))
        {
            return value is T typedValue ? typedValue : default;
        }
        return default;
    }

    public bool ContainsKey(string key)
    {
        return _parameters.ContainsKey(key);
    }

    /// <summary>
    ///     컬렉션 초기화 구문 지원을 위한 IEnumerable 구현
    /// </summary>
    public IEnumerator GetEnumerator()
    {
        return _parameters.GetEnumerator();
    }
}

