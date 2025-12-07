using System.Text.Json;
using Serilog;

namespace Jinobald.Core.Services.Settings;

/// <summary>
///     JSON 파일 기반 설정 서비스 구현
/// </summary>
public class JsonSettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly Dictionary<string, object?> _settings = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger _logger;
    private System.Timers.Timer? _saveTimer;
    private bool _isDirty;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     설정 값이 변경되었을 때 발생하는 이벤트
    /// </summary>
    public event Action<string, object?>? SettingChanged;

    public JsonSettingsService(string? settingsFilePath = null)
    {
        _settingsFilePath = settingsFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Jinobald",
            "settings.json");

        _logger = Log.ForContext<JsonSettingsService>();

        // 설정 파일 동기 로드 (생성자에서 async fire-and-forget 사용 시 race condition 발생)
        LoadSettingsSync();
    }

    /// <summary>
    ///     설정 값을 가져옵니다.
    /// </summary>
    public T Get<T>(string key, T defaultValue = default!)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("설정 키는 비어있을 수 없습니다.", nameof(key));

        _lock.Wait();
        try
        {
            if (_settings.TryGetValue(key, out var value) && value != null)
            {
                // JsonElement를 실제 타입으로 변환
                if (value is JsonElement element)
                {
                    return element.Deserialize<T>(_jsonOptions) ?? defaultValue;
                }

                // 직접 타입 변환 시도
                if (value is T typedValue)
                {
                    return typedValue;
                }

                // 타입 변환 시도
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    _logger.Warning("설정 '{Key}'의 값을 {Type} 타입으로 변환할 수 없습니다. 기본값을 반환합니다.", key, typeof(T).Name);
                    return defaultValue;
                }
            }

            return defaultValue;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     설정 값을 저장합니다.
    /// </summary>
    public void Set<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("설정 키는 비어있을 수 없습니다.", nameof(key));

        _lock.Wait();
        try
        {
            var oldValue = _settings.ContainsKey(key) ? _settings[key] : null;
            _settings[key] = value;

            // 변경 이벤트 발생
            if (!Equals(oldValue, value))
            {
                SettingChanged?.Invoke(key, value);
                _isDirty = true;
            }

            // Debounced 자동 저장 (500ms 후)
            ScheduleSave();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     설정 값이 존재하는지 확인합니다.
    /// </summary>
    public bool Contains(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        _lock.Wait();
        try
        {
            return _settings.ContainsKey(key);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     설정 값을 삭제합니다.
    /// </summary>
    public bool Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        _lock.Wait();
        try
        {
            var removed = _settings.Remove(key);
            if (removed)
            {
                SettingChanged?.Invoke(key, null);
                _isDirty = true;
                ScheduleSave();
            }

            return removed;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     모든 설정을 삭제합니다.
    /// </summary>
    public void Clear()
    {
        _lock.Wait();
        try
        {
            _settings.Clear();
            _isDirty = true;
            ScheduleSave();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     모든 설정 키를 가져옵니다.
    /// </summary>
    public IEnumerable<string> GetAllKeys()
    {
        _lock.Wait();
        try
        {
            return _settings.Keys.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     설정을 디스크에 저장합니다.
    ///     (내부적으로 debouncing이 적용되어 있으므로 직접 호출할 필요는 없습니다)
    /// </summary>
    public async Task SaveAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!_isDirty)
                return;

            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            _isDirty = false;
            _logger.Debug("설정 파일 저장됨: {FilePath}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "설정 파일 저장 실패: {FilePath}", _settingsFilePath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     지연된 저장을 예약합니다 (Debouncing)
    /// </summary>
    private void ScheduleSave()
    {
        // 기존 타이머 중지
        _saveTimer?.Stop();
        _saveTimer?.Dispose();

        // 500ms 후 저장
        _saveTimer = new System.Timers.Timer(500);
        _saveTimer.AutoReset = false;
        _saveTimer.Elapsed += async (s, e) =>
        {
            await SaveAsync();
        };
        _saveTimer.Start();
    }

    /// <summary>
    ///     설정을 디스크에서 다시 로드합니다.
    /// </summary>
    public async Task ReloadAsync()
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            LoadSettingsInternal();
        }
        finally
        {
            _lock.Release();
        }
    }

    private void LoadSettingsSync()
    {
        _lock.Wait();
        try
        {
            LoadSettingsInternal();
        }
        finally
        {
            _lock.Release();
        }
    }

    private void LoadSettingsInternal()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.Debug("설정 파일이 존재하지 않습니다. 새로운 설정 파일이 생성됩니다: {FilePath}", _settingsFilePath);
                return;
            }

            var json = File.ReadAllText(_settingsFilePath);
            var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonOptions);

            if (loadedSettings != null)
            {
                _settings.Clear();
                foreach (var kvp in loadedSettings)
                {
                    _settings[kvp.Key] = kvp.Value;
                }

                _logger.Information("설정 파일 로드됨: {FilePath} ({Count}개 설정)", _settingsFilePath, _settings.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "설정 파일 로드 실패: {FilePath}", _settingsFilePath);
        }
    }
}
