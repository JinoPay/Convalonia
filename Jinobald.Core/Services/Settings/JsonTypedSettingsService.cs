using System.Text.Json;
using Serilog;

namespace Jinobald.Core.Services.Settings;

/// <summary>
///     JSON 파일 기반 Strongly-Typed 설정 서비스 구현.
///     자동 저장(debouncing)과 변경 알림을 지원합니다.
/// </summary>
/// <typeparam name="TSettings">설정 POCO 클래스 타입</typeparam>
public class JsonTypedSettingsService<TSettings> : ITypedSettingsService<TSettings>, IDisposable
    where TSettings : class, new()
{
    private readonly string _settingsFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger _logger;
    private TSettings _settings;
    private System.Timers.Timer? _saveTimer;
    private bool _isDirty;
    private bool _disposed;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    public event Action<TSettings>? SettingsChanged;

    /// <inheritdoc />
    public TSettings Value
    {
        get
        {
            _lock.Wait();
            try
            {
                return _settings;
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    /// <summary>
    ///     JsonTypedSettingsService 생성자
    /// </summary>
    /// <param name="settingsFilePath">설정 파일 경로 (null인 경우 기본 경로 사용)</param>
    /// <param name="fileName">설정 파일 이름 (settingsFilePath가 null인 경우 사용)</param>
    public JsonTypedSettingsService(string? settingsFilePath = null, string? fileName = null)
    {
        var actualFileName = fileName ?? $"{typeof(TSettings).Name.ToLowerInvariant()}.json";

        _settingsFilePath = settingsFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Jinobald",
            actualFileName);

        _logger = Log.ForContext<JsonTypedSettingsService<TSettings>>();
        _settings = new TSettings();

        LoadSettingsSync();
    }

    /// <inheritdoc />
    public void Update(Action<TSettings> updateAction)
    {
        ArgumentNullException.ThrowIfNull(updateAction);

        _lock.Wait();
        try
        {
            updateAction(_settings);
            _isDirty = true;
            ScheduleSave();
        }
        finally
        {
            _lock.Release();
        }

        SettingsChanged?.Invoke(_settings);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Func<TSettings, Task> updateAction)
    {
        ArgumentNullException.ThrowIfNull(updateAction);

        await _lock.WaitAsync();
        try
        {
            await updateAction(_settings);
            _isDirty = true;
            ScheduleSave();
        }
        finally
        {
            _lock.Release();
        }

        SettingsChanged?.Invoke(_settings);
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await SaveInternalAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task ReloadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await LoadSettingsInternalAsync();
        }
        finally
        {
            _lock.Release();
        }

        SettingsChanged?.Invoke(_settings);
    }

    /// <inheritdoc />
    public void Reset()
    {
        _lock.Wait();
        try
        {
            _settings = new TSettings();
            _isDirty = true;
            ScheduleSave();
        }
        finally
        {
            _lock.Release();
        }

        SettingsChanged?.Invoke(_settings);
    }

    private async Task SaveInternalAsync()
    {
        if (!_isDirty)
            return;

        try
        {
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
    }

    private void ScheduleSave()
    {
        _saveTimer?.Stop();
        _saveTimer?.Dispose();

        _saveTimer = new System.Timers.Timer(500);
        _saveTimer.AutoReset = false;
        _saveTimer.Elapsed += async (_, _) =>
        {
            await _lock.WaitAsync();
            try
            {
                await SaveInternalAsync();
            }
            finally
            {
                _lock.Release();
            }
        };
        _saveTimer.Start();
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

    private async Task LoadSettingsInternalAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.Debug("설정 파일이 존재하지 않습니다. 기본값을 사용합니다: {FilePath}", _settingsFilePath);
                _settings = new TSettings();
                return;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var loaded = JsonSerializer.Deserialize<TSettings>(json, _jsonOptions);
            _settings = loaded ?? new TSettings();

            _logger.Information("설정 파일 로드됨: {FilePath}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "설정 파일 로드 실패: {FilePath}", _settingsFilePath);
            _settings = new TSettings();
        }
    }

    private void LoadSettingsInternal()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.Debug("설정 파일이 존재하지 않습니다. 기본값을 사용합니다: {FilePath}", _settingsFilePath);
                _settings = new TSettings();
                return;
            }

            var json = File.ReadAllText(_settingsFilePath);
            var loaded = JsonSerializer.Deserialize<TSettings>(json, _jsonOptions);
            _settings = loaded ?? new TSettings();

            _logger.Information("설정 파일 로드됨: {FilePath}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "설정 파일 로드 실패: {FilePath}", _settingsFilePath);
            _settings = new TSettings();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _saveTimer?.Stop();
        _saveTimer?.Dispose();
        _lock.Dispose();

        GC.SuppressFinalize(this);
    }
}
