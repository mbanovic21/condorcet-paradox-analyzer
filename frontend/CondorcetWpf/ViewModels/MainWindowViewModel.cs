using System.ComponentModel;
using System.Runtime.CompilerServices;
using CondorcetWpf.Services;

namespace CondorcetWpf.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ApiClient _api;

    public string BackendBaseUrl { get; }

    private string _status = "Load a JSON file, then click Analyze (backend must be running).";
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

    private bool _saveArtifact = true;
    public bool SaveArtifact
    {
        get => _saveArtifact;
        set { _saveArtifact = value; OnPropertyChanged(); AnalyzeVm.SaveArtifact = value; }
    }

    private bool _includeTrace = true;
    public bool IncludeTrace
    {
        get => _includeTrace;
        set { _includeTrace = value; OnPropertyChanged(); AnalyzeVm.IncludeTrace = value; }
    }

    private string _artifactTag = "run";
    public string ArtifactTag
    {
        get => _artifactTag;
        set { _artifactTag = value; OnPropertyChanged(); AnalyzeVm.ArtifactTag = value; }
    }

    private string _artifactNotes = "";
    public string ArtifactNotes
    {
        get => _artifactNotes;
        set { _artifactNotes = value; OnPropertyChanged(); AnalyzeVm.ArtifactNotes = value; }
    }

    public bool HasInput => AnalyzeVm.HasInput;

    public AnalyzeViewModel AnalyzeVm { get; }
    public DfsSimulationViewModel DfsSimulationVm { get; }
    public DashboardViewModel DashboardVm { get; }
    public ArtifactsViewModel ArtifactsVm { get; }
    public SettingsViewModel SettingsVm { get; }

    private object _currentView;
    public object CurrentView { get => _currentView; set { _currentView = value; OnPropertyChanged(); } }

    public RelayCommand LoadJsonCommand { get; }
    public RelayCommand AnalyzeCommand { get; }

    public RelayCommand NavigateDashboardCommand { get; }
    public RelayCommand NavigateAnalyzeCommand { get; }
    public RelayCommand NavigateDfsSimulationCommand { get; }
    public RelayCommand NavigateArtifactsCommand { get; }
    public RelayCommand NavigateSettingsCommand { get; }

    public MainWindowViewModel()
    {
        BackendBaseUrl = "http://127.0.0.1:8000";
        _api = new ApiClient(BackendBaseUrl);

        AnalyzeVm = new AnalyzeViewModel(
            _api,
            setStatus: s => Status = s,
            notifyHasInputChanged: () => OnPropertyChanged(nameof(HasInput))
        );

        AnalyzeVm.IncludeTrace = this.IncludeTrace;
        AnalyzeVm.SaveArtifact = this.SaveArtifact;
        AnalyzeVm.ArtifactTag = this.ArtifactTag;
        AnalyzeVm.ArtifactNotes = this.ArtifactNotes;

        DashboardVm = new DashboardViewModel(AnalyzeVm);
        DfsSimulationVm = new DfsSimulationViewModel(AnalyzeVm);
        ArtifactsVm = new ArtifactsViewModel(AnalyzeVm);
        SettingsVm = new SettingsViewModel(this);

        DashboardVm = new DashboardViewModel(AnalyzeVm);
        DfsSimulationVm = new DfsSimulationViewModel(AnalyzeVm);
        ArtifactsVm = new ArtifactsViewModel(AnalyzeVm);
        SettingsVm = new SettingsViewModel(this);

        LoadJsonCommand = AnalyzeVm.LoadJsonCommand;
        AnalyzeCommand = AnalyzeVm.AnalyzeCommand;

        NavigateDashboardCommand = new RelayCommand(() => CurrentView = DashboardVm);
        NavigateAnalyzeCommand = new RelayCommand(() => CurrentView = AnalyzeVm);
        NavigateDfsSimulationCommand = new RelayCommand(() => CurrentView = DfsSimulationVm);
        NavigateArtifactsCommand = new RelayCommand(() => CurrentView = ArtifactsVm);
        NavigateSettingsCommand = new RelayCommand(() => CurrentView = SettingsVm);

        _currentView = DashboardVm;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}