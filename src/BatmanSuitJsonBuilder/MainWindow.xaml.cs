using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using WinMessageBox = System.Windows.MessageBox;
using WinMessageBoxButton = System.Windows.MessageBoxButton;
using WinMessageBoxImage = System.Windows.MessageBoxImage;
using WinMessageBoxResult = System.Windows.MessageBoxResult;
using System.Windows.Data;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace BatmanSuitJsonBuilder;

public partial class MainWindow : FluentWindow, INotifyPropertyChanged
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly DiscordPresenceService _discordPresence = new();

    private const string DefaultExportSlotId = "custom_batman_suit";
    private const string DefaultExportDisplayName = "Custom Batman Suit";

    private EditableFieldRegistry _registry = EditableFieldRegistry.Default();
    private string? _loadedPath;
    private string? _loadedSuitConfigPath;
    private string? _loadedPlayablePath;
    private string? _loadedPlayableActorClass;
    private string? _loadedPlayableSourceTag;
    private string _slotId = "";
    private string _displayName = "";
    private string _description = "";
    private string _sourceTag = "Pawns.Playable.Batman.TheBatman2025";
    private string _sourceActorClassContains = "BP_Batman_TheBatman2025_Playable_C";
    private string _iconAsset = "";
    private string _uimdMenuIcon = "";
    private string _uimdRightFacing = "";
    private string _uimdLeftFacing = "";
    private string _menuOrder = "100";
    private bool _isEnabled = true;
    private string _statusText = "Drop an exported playable JSON or existing suit.json anywhere on this window.";
    private string _operationFilter = "";
    private string _detectionFilter = "";
    private string _knownFieldFilter = "";
    private OperationRow? _selectedOperation;
    private EquipmentRow? _selectedEquipment;
    private KnownEditableField? _selectedKnownField;

    public MainWindow()
    {
        InitializeComponent();
        LoadRegistry();
        _discordPresence.Initialize();

        FilteredOperations = CollectionViewSource.GetDefaultView(OperationRows);
        FilteredOperations.Filter = FilterOperation;
        FilteredDetections = CollectionViewSource.GetDefaultView(DetectedFields);
        FilteredDetections.Filter = FilterDetection;
        FilteredKnownFields = CollectionViewSource.GetDefaultView(KnownFieldRows);
        FilteredKnownFields.Filter = FilterKnownField;

        DataContext = this;
        StatusText = $"Ready. Loaded registry with {PawnTagPresets.Count} pawn presets, {ComponentChoices.Count} components, {TextureParameterChoices.Count} texture params, {VectorParameterChoices.Count} vector params, {ScalarParameterChoices.Count} scalar params, and {OperationTypeChoices.Count} operation types.";
        _discordPresence.SetPageActivity(0, DisplayName);

        Dispatcher.BeginInvoke(() => SelectMainPage(0));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<OperationRow> OperationRows { get; } = new();
    public ObservableCollection<EquipmentRow> EquipmentRows { get; } = new();
    public ObservableCollection<DetectedField> DetectedFields { get; } = new();
    public ObservableCollection<KnownEditableField> KnownFieldRows { get; } = new();
    public ICollectionView FilteredOperations { get; }
    public ICollectionView FilteredDetections { get; }
    public ICollectionView FilteredKnownFields { get; }

    public ObservableCollection<string> OperationTypeChoices { get; } = new();
    public ObservableCollection<string> ComponentChoices { get; } = new();
    public ObservableCollection<string> ComponentClassChoices { get; } = new();
    public ObservableCollection<string> TextureParameterChoices { get; } = new();
    public ObservableCollection<string> VectorParameterChoices { get; } = new();
    public ObservableCollection<string> ScalarParameterChoices { get; } = new();
    public ObservableCollection<string> ApplyToChoices { get; } = new();
    public ObservableCollection<string> PawnTagPresets { get; } = new();
    public ObservableCollection<string> ActorClassPresets { get; } = new();

    public IEnumerable<string> SelectedParameterChoices => SelectedOperation?.Type switch
    {
        "set_vector_parameter" => VectorParameterChoices,
        "set_scalar_parameter" => ScalarParameterChoices,
        _ => TextureParameterChoices
    };

    public string SlotId
    {
        get => _slotId;
        set => SetProperty(ref _slotId, CleanSlotIdText(value));
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, CleanDisplayNameText(value));
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string SourceTag
    {
        get => _sourceTag;
        set
        {
            if (SetProperty(ref _sourceTag, value))
            {
                SyncActorClassFromPawnTag(value);
            }
        }
    }

    public string SourceActorClassContains
    {
        get => _sourceActorClassContains;
        set => SetProperty(ref _sourceActorClassContains, value);
    }

    public string IconAsset
    {
        get => _iconAsset;
        set => SetProperty(ref _iconAsset, value);
    }

    public string UimdMenuIcon
    {
        get => _uimdMenuIcon;
        set => SetProperty(ref _uimdMenuIcon, value);
    }

    public string UimdRightFacing
    {
        get => _uimdRightFacing;
        set => SetProperty(ref _uimdRightFacing, value);
    }

    public string UimdLeftFacing
    {
        get => _uimdLeftFacing;
        set => SetProperty(ref _uimdLeftFacing, value);
    }

    public string MenuOrder
    {
        get => _menuOrder;
        set => SetProperty(ref _menuOrder, value);
    }

    public bool SuitEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string OperationFilter
    {
        get => _operationFilter;
        set
        {
            if (SetProperty(ref _operationFilter, value))
            {
                FilteredOperations.Refresh();
            }
        }
    }

    public string DetectionFilter
    {
        get => _detectionFilter;
        set
        {
            if (SetProperty(ref _detectionFilter, value))
            {
                FilteredDetections.Refresh();
            }
        }
    }

    public string KnownFieldFilter
    {
        get => _knownFieldFilter;
        set
        {
            if (SetProperty(ref _knownFieldFilter, value))
            {
                FilteredKnownFields.Refresh();
            }
        }
    }

    public KnownEditableField? SelectedKnownField
    {
        get => _selectedKnownField;
        set => SetProperty(ref _selectedKnownField, value);
    }

    public OperationRow? SelectedOperation
    {
        get => _selectedOperation;
        set
        {
            if (ReferenceEquals(_selectedOperation, value))
            {
                return;
            }

            if (_selectedOperation is not null)
            {
                _selectedOperation.PropertyChanged -= SelectedOperation_PropertyChanged;
            }

            _selectedOperation = value;

            if (_selectedOperation is not null)
            {
                _selectedOperation.PropertyChanged += SelectedOperation_PropertyChanged;
                NormalizeSelectedOperationParameter();
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedParameterChoices));
        }
    }

    public EquipmentRow? SelectedEquipment
    {
        get => _selectedEquipment;
        set => SetProperty(ref _selectedEquipment, value);
    }

    private void SelectedOperation_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OperationRow.Type))
        {
            NormalizeSelectedOperationParameter();
            OnPropertyChanged(nameof(SelectedParameterChoices));
        }
    }

    private OperationRow? GetOperationRowFromMenuItem(object sender)
    {
        if (sender is FrameworkElement menuItem
            && menuItem.Parent is System.Windows.Controls.ContextMenu contextMenu
            && contextMenu.PlacementTarget is FrameworkElement placementTarget
            && placementTarget.DataContext is OperationRow row)
        {
            return row;
        }

        if (sender is FrameworkElement directElement && directElement.DataContext is OperationRow directRow)
        {
            return directRow;
        }

        return SelectedOperation;
    }

    private void CopyOperationValue_Click(object sender, RoutedEventArgs e)
    {
        var row = GetOperationRowFromMenuItem(sender);
        if (row is null)
        {
            return;
        }

        SelectedOperation = row;
        Clipboard.SetDataObject(row.Asset ?? string.Empty, true);
        StatusText = "Copied operation value.";
    }

    private void PasteOperationValue_Click(object sender, RoutedEventArgs e)
    {
        var row = GetOperationRowFromMenuItem(sender);
        if (row is null || !Clipboard.ContainsText())
        {
            return;
        }

        SelectedOperation = row;
        row.Asset = Clipboard.GetText();
        StatusText = "Pasted operation value.";
    }

    private void NormalizeSelectedOperationParameter()
    {
        if (SelectedOperation is null)
        {
            return;
        }

        if (SelectedOperation.Type == "set_vector_parameter")
        {
            if (!VectorParameterChoices.Contains(SelectedOperation.ParameterName, StringComparer.OrdinalIgnoreCase))
            {
                SelectedOperation.ParameterName = VectorParameterChoices.FirstOrDefault() ?? "Base Color";
            }

            if (string.IsNullOrWhiteSpace(SelectedOperation.Asset))
            {
                SelectedOperation.Asset = "1.0, 1.0, 1.0, 1.0";
            }
        }
        else if (SelectedOperation.Type == "set_scalar_parameter")
        {
            if (!ScalarParameterChoices.Contains(SelectedOperation.ParameterName, StringComparer.OrdinalIgnoreCase))
            {
                SelectedOperation.ParameterName = ScalarParameterChoices.FirstOrDefault() ?? "ColourChannel_Picker";
            }

            if (string.IsNullOrWhiteSpace(SelectedOperation.Asset))
            {
                SelectedOperation.Asset = "0.0";
            }
        }
        else if (SelectedOperation.Type == "set_texture_parameter")
        {
            if (!TextureParameterChoices.Contains(SelectedOperation.ParameterName, StringComparer.OrdinalIgnoreCase))
            {
                SelectedOperation.ParameterName = TextureParameterChoices.FirstOrDefault() ?? "BC";
            }
        }
    }

    private void OpenJson_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open exported playable JSON or NewSuitSlotNative suit.json",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            LoadJsonFiles(dialog.FileNames);
        }
    }

    private void OpenSuitJson_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import existing NewSuitSlotNative suit.json",
            Filter = "Suit JSON files (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            LoadSuitJsonFile(dialog.FileName);
        }
    }

    private void OpenPlayableJson_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import exported playable JSON",
            Filter = "Playable JSON files (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            LoadPlayableJsonFile(dialog.FileName);
        }
    }

    private void ExportConfig_Click(object sender, RoutedEventArgs e)
    {
        var activeOperations = OperationRows.Where(o => o.Enabled).ToList();
        var activeEquipment = EquipmentRows.Where(e => e.Enabled).ToList();

        if (activeOperations.Count == 0 && activeEquipment.Count == 0)
        {
            WinMessageBox.Show(this, "Add at least one operation or equipment replacement before exporting.", "Nothing to export", WinMessageBoxButton.OK, WinMessageBoxImage.Information);
            return;
        }

        var missingAssets = activeOperations
            .Where(o => OperationNeedsAsset(o.Type) && string.IsNullOrWhiteSpace(o.Asset))
            .ToList();

        if (missingAssets.Count > 0)
        {
            var answer = WinMessageBox.Show(this,
                $"{missingAssets.Count} enabled operation(s) are missing an Asset / Value field. Export anyway?",
                "Missing values",
                WinMessageBoxButton.YesNo,
                WinMessageBoxImage.Warning);

            if (answer != WinMessageBoxResult.Yes)
            {
                return;
            }
        }

        var exportSlotId = string.IsNullOrWhiteSpace(SlotId) ? DefaultExportSlotId : SlotId.Trim();
        var exportDisplayName = string.IsNullOrWhiteSpace(DisplayName) ? exportSlotId : DisplayName.Trim();

        var config = new ExportSuitConfig
        {
            SchemaVersion = 2,
            Enabled = SuitEnabled,
            MenuOrder = ParseInt(MenuOrder, 100),
            SlotId = exportSlotId,
            DisplayName = exportDisplayName,
            Description = Description,
            SourceTag = SourceTag,
            SourceActorClassContains = SourceActorClassContains,
            IconAsset = string.IsNullOrWhiteSpace(IconAsset) ? null : IconAsset.Trim(),
            UimdIcons = BuildUimdIconsConfig(),
            Operations = activeOperations.Select(BuildOperationDictionary).Where(d => d.Count > 0).ToList(),
            EquipmentReplacements = activeEquipment.Select(e => new ExportEquipmentReplacement
            {
                Slot = ParseInt(e.Slot, 0),
                ReplaceEquipment = e.ReplaceEquipment,
                WithEquipment = e.WithEquipment
            }).ToList()
        };

        var dialog = new SaveFileDialog
        {
            Title = "Export NewSuitSlotNative suit.json",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = $"{exportSlotId}.json",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(config, _jsonOptions));
            StatusText = $"Exported {activeOperations.Count} operation(s) and {activeEquipment.Count} equipment replacement(s) to {dialog.FileName}.";
            _discordPresence.SetActivity("Exported suit.json", exportDisplayName);
        }
    }


    private UimdIconsConfig? BuildUimdIconsConfig()
    {
        var menuIcon = UimdMenuIcon.Trim();
        var rightFacing = UimdRightFacing.Trim();
        var leftFacing = UimdLeftFacing.Trim();

        if (string.IsNullOrWhiteSpace(menuIcon)
            && string.IsNullOrWhiteSpace(rightFacing)
            && string.IsNullOrWhiteSpace(leftFacing))
        {
            return null;
        }

        return new UimdIconsConfig
        {
            MenuIcon = string.IsNullOrWhiteSpace(menuIcon) ? null : menuIcon,
            RightFacing = string.IsNullOrWhiteSpace(rightFacing) ? null : rightFacing,
            LeftFacing = string.IsNullOrWhiteSpace(leftFacing) ? null : leftFacing
        };
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _loadedPath = null;
        _loadedSuitConfigPath = null;
        _loadedPlayablePath = null;
        _loadedPlayableActorClass = null;
        _loadedPlayableSourceTag = null;
        SlotId = "";
        DisplayName = "";
        Description = "";
        SourceTag = "Pawns.Playable.Batman.TheBatman2025";
        SourceActorClassContains = "BP_Batman_TheBatman2025_Playable_C";
        IconAsset = "";
        UimdMenuIcon = "";
        UimdRightFacing = "";
        UimdLeftFacing = "";
        MenuOrder = "100";
        SuitEnabled = true;
        OperationRows.Clear();
        EquipmentRows.Clear();
        DetectedFields.Clear();
        KnownFieldRows.Clear();
        SelectedKnownField = null;
        StatusText = "Cleared. Drop an exported playable JSON or existing suit.json to begin.";
        SelectMainPage(0);
    }

    private void SelectMainPage(int index)
    {
        if (MainPagesTabControl != null)
        {
            MainPagesTabControl.SelectedIndex = Math.Clamp(index, 0, 3);
            _discordPresence.SetPageActivity(MainPagesTabControl.SelectedIndex, DisplayName);
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e) => SelectMainPage(0);

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _discordPresence.Dispose();
    }

    private void AddTexture_Click(object sender, RoutedEventArgs e) => AddOperation("set_texture_parameter");
    private void AddMaterial_Click(object sender, RoutedEventArgs e) => AddOperation("set_material");
    private void AddStaticMesh_Click(object sender, RoutedEventArgs e) => AddOperation("set_static_mesh");
    private void AddVisibility_Click(object sender, RoutedEventArgs e) => AddOperation("set_visibility");
    private void AddHidden_Click(object sender, RoutedEventArgs e) => AddOperation("set_hidden_in_game");
    private void AddAttach_Click(object sender, RoutedEventArgs e) => AddOperation("attach_component");
    private void AddEnsure_Click(object sender, RoutedEventArgs e) => AddOperation("ensure_component");

    private void AddKnownTexture_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("set_texture_parameter");
    private void AddKnownVector_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("set_vector_parameter");
    private void AddKnownScalar_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("set_scalar_parameter");
    private void AddKnownMaterial_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("set_material");
    private void AddKnownMesh_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("set_static_mesh");
    private void AddKnownSkeletalMesh_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("set_skeletal_mesh");
    private void AddKnownAnimClass_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("set_anim_class");
    private void AddKnownClearStaticMesh_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("clear_static_mesh");
    private void AddKnownClearSkeletalMesh_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("clear_skeletal_mesh");
    private void AddKnownCreateSkeletal_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("create_skeletal_component");
    private void AddKnownVisibility_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("set_visibility");
    private void AddKnownHidden_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("set_hidden_in_game");
    private void AddKnownAttach_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("attach_component");
    private void AddKnownEnsure_Click(object sender, RoutedEventArgs e) => AddOperationFromKnownField("ensure_component");
    private void AddKnownEquipment_Click(object sender, RoutedEventArgs e) => AddEquipmentFromKnownField();

    private void AddEquipment_Click(object sender, RoutedEventArgs e)
    {
        EquipmentRows.Add(new EquipmentRow
        {
            Enabled = true,
            Slot = "0",
            ReplaceEquipment = "/Game/Characters/Equipment/Batarang/DA_ETA_Batarang.DA_ETA_Batarang",
            WithEquipment = ""
        });
        StatusText = "Added equipment replacement row.";
    }

    private void DuplicateSelectedOperation_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedOperation is null)
        {
            return;
        }

        OperationRows.Add(SelectedOperation.Clone());
        StatusText = "Duplicated selected operation.";
    }

    private void DeleteSelectedOperations_Click(object sender, RoutedEventArgs e)
    {
        var selected = OperationRows.Where(o => o.IsMarkedForDelete || ReferenceEquals(o, SelectedOperation)).Distinct().ToList();
        if (selected.Count == 0 && SelectedOperation is not null)
        {
            selected.Add(SelectedOperation);
        }

        foreach (var row in selected)
        {
            OperationRows.Remove(row);
        }

        StatusText = $"Deleted {selected.Count} operation row(s).";
    }

    private void DeleteSelectedEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedEquipment is null)
        {
            return;
        }

        EquipmentRows.Remove(SelectedEquipment);
        StatusText = "Deleted selected equipment row.";
    }

    private void AddSelectedDetections_Click(object sender, RoutedEventArgs e)
    {
        var selected = DetectedFields.Where(d => d.IsSelected).ToList();
        if (selected.Count == 0)
        {
            WinMessageBox.Show(this, "Select one or more detected fields first.", "No detections selected", WinMessageBoxButton.OK, WinMessageBoxImage.Information);
            return;
        }

        var addedOps = 0;
        var addedEquipment = 0;

        foreach (var detection in selected)
        {
            if (detection.Kind == "Equipment")
            {
                EquipmentRows.Add(new EquipmentRow
                {
                    Enabled = true,
                    Slot = "0",
                    ReplaceEquipment = detection.CurrentValue,
                    WithEquipment = "",
                    Notes = detection.JsonPath
                });
                addedEquipment++;
                continue;
            }

            var row = CreateOperationRow(detection.SuggestedOperationType, detection.Component, detection.ParameterName);
            row.Asset = "";
            row.Notes = $"Detected current value: {Shorten(detection.CurrentValue, 140)}";
            OperationRows.Add(row);
            addedOps++;
        }

        StatusText = $"Added {addedOps} operation row(s) and {addedEquipment} equipment row(s) from detections.";
    }

    private void ClearDetections_Click(object sender, RoutedEventArgs e)
    {
        DetectedFields.Clear();
        StatusText = "Cleared detection results.";
    }

    private void OpenRegistryFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "Data");
            Directory.CreateDirectory(folder);
            EnsureRegistryFileExists(folder);
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            WinMessageBox.Show(this, ex.Message, "Could not open registry folder", WinMessageBoxButton.OK, WinMessageBoxImage.Error);
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            LoadJsonFiles(files);
        }
    }

    private void LoadJsonFiles(IEnumerable<string> paths)
    {
        var jsonFiles = paths
            .Where(f => string.Equals(Path.GetExtension(f), ".json", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (jsonFiles.Count == 0)
        {
            return;
        }

        if (jsonFiles.Count == 1)
        {
            LoadJsonFile(jsonFiles[0]);
            return;
        }

        var loaded = new List<PendingJsonFile>();
        try
        {
            foreach (var jsonFile in jsonFiles)
            {
                var text = File.ReadAllText(jsonFile);
                var doc = JsonDocument.Parse(text, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });

                loaded.Add(new PendingJsonFile(jsonFile, doc, LooksLikeNewSuitSlotConfig(doc.RootElement)));
            }

            var suitConfigs = loaded.Where(f => f.IsSuitConfig).ToList();
            var playableExports = loaded.Where(f => !f.IsSuitConfig).ToList();

            if (suitConfigs.Count == 1 && playableExports.Count == 1)
            {
                var suitConfig = suitConfigs[0];
                var playableExport = playableExports[0];

                if (!SuitConfigMatchesPlayable(suitConfig.Document.RootElement, playableExport.Document.RootElement, playableExport.Path, out var mismatchReason))
                {
                    WinMessageBox.Show(this,
                        mismatchReason,
                        "Suit/playable mismatch",
                        WinMessageBoxButton.OK,
                        WinMessageBoxImage.Warning);
                    StatusText = "Import cancelled because the suit.json target does not match the playable JSON.";
                    return;
                }

                ImportSuitConfig(suitConfig.Document.RootElement, suitConfig.Path);
                ScanKnownFields(playableExport.Document.RootElement, playableExport.Path, preserveExistingRows: true);
                _loadedPath = $"{suitConfig.Path}; {playableExport.Path}";
                StatusText = $"Imported {Path.GetFileName(suitConfig.Path)} and matched playable {Path.GetFileName(playableExport.Path)}. Existing operations were kept, and playable parts are available for adding more changes.";
                _discordPresence.SetActivity("Editing suit.json", string.IsNullOrWhiteSpace(DisplayName) ? Path.GetFileNameWithoutExtension(suitConfig.Path) : DisplayName);
                return;
            }

            WinMessageBox.Show(this,
                "Open or drop exactly one existing suit.json and one exported playable JSON together. The suit.json source_tag/source_actor_class_contains must match that playable export.",
                "Unsupported multi-import",
                WinMessageBoxButton.OK,
                WinMessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            WinMessageBox.Show(this, ex.Message, "Could not load JSON files", WinMessageBoxButton.OK, WinMessageBoxImage.Error);
            StatusText = "Failed to load JSON files.";
        }
        finally
        {
            foreach (var item in loaded)
            {
                item.Document.Dispose();
            }
        }
    }

    private void LoadJsonFile(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            _loadedPath = path;
            DetectedFields.Clear();

            if (LooksLikeNewSuitSlotConfig(doc.RootElement))
            {
                var keepPlayableFields = !string.IsNullOrWhiteSpace(_loadedPlayablePath)
                                         && SuitConfigMatchesLoadedPlayable(doc.RootElement, out _);
                ImportSuitConfig(doc.RootElement, path, preserveKnownFields: keepPlayableFields);

                if (keepPlayableFields)
                {
                    StatusText = $"Imported existing suit config {Path.GetFileName(path)} and kept matched playable fields from {Path.GetFileName(_loadedPlayablePath)} for adding more changes.";
                }
            }
            else
            {
                var hasLoadedSuitConfig = !string.IsNullOrWhiteSpace(_loadedSuitConfigPath);
                if (hasLoadedSuitConfig && !CurrentSuitConfigMatchesPlayable(doc.RootElement, path, out var mismatchReason))
                {
                    WinMessageBox.Show(this,
                        mismatchReason,
                        "Suit/playable mismatch",
                        WinMessageBoxButton.OK,
                        WinMessageBoxImage.Warning);
                    StatusText = "Playable import cancelled because it does not match the currently loaded suit.json target.";
                    return;
                }

                ScanKnownFields(doc.RootElement, path, preserveExistingRows: hasLoadedSuitConfig);

                if (hasLoadedSuitConfig)
                {
                    StatusText = $"Loaded matched playable {Path.GetFileName(path)}. Existing suit.json operations were kept, and playable parts are available for adding more changes.";
                }
            }
        }
        catch (Exception ex)
        {
            WinMessageBox.Show(this, ex.Message, "Could not load JSON", WinMessageBoxButton.OK, WinMessageBoxImage.Error);
            StatusText = "Failed to load JSON.";
        }
    }

    private void LoadSuitJsonFile(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            if (!LooksLikeNewSuitSlotConfig(doc.RootElement))
            {
                WinMessageBox.Show(this,
                    "That file does not look like an existing NewSuitSlotNative suit.json. Use Import Playable JSON for exported playable files.",
                    "Not a suit.json",
                    WinMessageBoxButton.OK,
                    WinMessageBoxImage.Warning);
                StatusText = "Suit import cancelled because the selected file is not a suit.json.";
                return;
            }

            _loadedPath = path;
            DetectedFields.Clear();

            var keepPlayableFields = !string.IsNullOrWhiteSpace(_loadedPlayablePath)
                                     && SuitConfigMatchesLoadedPlayable(doc.RootElement, out _);
            ImportSuitConfig(doc.RootElement, path, preserveKnownFields: keepPlayableFields);

            if (keepPlayableFields)
            {
                StatusText = $"Imported existing suit config {Path.GetFileName(path)} and kept matched playable fields from {Path.GetFileName(_loadedPlayablePath)} for adding more changes.";
            }
        }
        catch (Exception ex)
        {
            WinMessageBox.Show(this, ex.Message, "Could not load suit.json", WinMessageBoxButton.OK, WinMessageBoxImage.Error);
            StatusText = "Failed to load suit.json.";
        }
    }

    private void LoadPlayableJsonFile(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(text, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            if (LooksLikeNewSuitSlotConfig(doc.RootElement))
            {
                WinMessageBox.Show(this,
                    "That file looks like an existing suit.json. Use Import Suit.JSON for existing suit configs.",
                    "Not a playable JSON",
                    WinMessageBoxButton.OK,
                    WinMessageBoxImage.Warning);
                StatusText = "Playable import cancelled because the selected file is a suit.json.";
                return;
            }

            _loadedPath = path;
            DetectedFields.Clear();

            var hasLoadedSuitConfig = !string.IsNullOrWhiteSpace(_loadedSuitConfigPath);
            if (hasLoadedSuitConfig && !CurrentSuitConfigMatchesPlayable(doc.RootElement, path, out var mismatchReason))
            {
                WinMessageBox.Show(this,
                    mismatchReason,
                    "Suit/playable mismatch",
                    WinMessageBoxButton.OK,
                    WinMessageBoxImage.Warning);
                StatusText = "Playable import cancelled because it does not match the currently loaded suit.json target.";
                return;
            }

            ScanKnownFields(doc.RootElement, path, preserveExistingRows: hasLoadedSuitConfig);

            if (hasLoadedSuitConfig)
            {
                StatusText = $"Loaded matched playable {Path.GetFileName(path)}. Existing suit.json operations were kept, and playable parts are available for adding more changes.";
            }
        }
        catch (Exception ex)
        {
            WinMessageBox.Show(this, ex.Message, "Could not load playable JSON", WinMessageBoxButton.OK, WinMessageBoxImage.Error);
            StatusText = "Failed to load playable JSON.";
        }
    }

    private void ImportSuitConfig(JsonElement root, string path, bool preserveKnownFields = false)
    {
        SlotId = GetString(root, "slot_id") ?? "";
        DisplayName = GetString(root, "display_name") ?? "";
        Description = GetString(root, "description") ?? "";
        SourceTag = GetString(root, "source_tag") ?? SourceTag;
        SourceActorClassContains = GetString(root, "source_actor_class_contains") ?? SourceActorClassContains;
        IconAsset = GetString(root, "icon_asset") ?? "";
        ImportUimdIcons(root);
        MenuOrder = GetIntNullable(root, "menu_order")?.ToString() ?? "100";
        SuitEnabled = GetBoolNullable(root, "enabled") ?? true;
        _loadedSuitConfigPath = path;
        if (!preserveKnownFields)
        {
            _loadedPlayablePath = null;
            _loadedPlayableActorClass = null;
            _loadedPlayableSourceTag = null;
        }

        OperationRows.Clear();
        EquipmentRows.Clear();

        if (root.TryGetProperty("operations", out var operations) && operations.ValueKind == JsonValueKind.Array)
        {
            foreach (var op in operations.EnumerateArray())
            {
                var row = OperationRow.FromJson(op, _registry);
                OperationRows.Add(row);
            }
        }

        if (root.TryGetProperty("equipment_replacements", out var equipment) && equipment.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in equipment.EnumerateArray())
            {
                EquipmentRows.Add(EquipmentRow.FromJson(entry));
            }
        }

        if (!preserveKnownFields)
        {
            BuildKnownFieldsFromOperations();
        }
        else if (SelectedKnownField is null)
        {
            SelectedKnownField = KnownFieldRows.FirstOrDefault();
        }

        StatusText = $"Imported existing suit config {Path.GetFileName(path)} with {OperationRows.Count} operation(s) and {EquipmentRows.Count} equipment replacement(s).";
        _discordPresence.SetActivity("Editing suit.json", DisplayName);
        FilteredOperations.Refresh();
        FilteredDetections.Refresh();
        FilteredKnownFields.Refresh();
    }

    private void ImportUimdIcons(JsonElement root)
    {
        UimdMenuIcon = "";
        UimdRightFacing = "";
        UimdLeftFacing = "";

        if (!root.TryGetProperty("uimd_icons", out var icons) || icons.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        UimdMenuIcon = GetString(icons, "menu_icon") ?? "";
        UimdRightFacing = GetString(icons, "right_facing") ?? "";
        UimdLeftFacing = GetString(icons, "left_facing") ?? "";
    }

    private void ScanKnownFields(JsonElement root, string path, bool preserveExistingRows = false)
    {
        if (!preserveExistingRows)
        {
            OperationRows.Clear();
            EquipmentRows.Clear();
            _loadedSuitConfigPath = null;
        }

        DetectedFields.Clear();
        KnownFieldRows.Clear();

        // Playable exports are arrays of UObject records. The old scanner
        // looked at every string path and could accidentally assign nested
        // Head/Cape/Torso values to CharacterMesh0 just because those SCS
        // nodes use CharacterMesh0 as their parent. Prefer a strict component
        // pass first: only actual component records create known fields.
        var usedStrictPlayableScan = TryScanPlayableExportKnownFields(root);

        if (!usedStrictPlayableScan)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            TraverseForDetections(root, "$", "root", seen);
        }

        foreach (var detection in DetectedFields)
        {
            if (detection.Kind is "Texture" or "Texture Param" or "Material" or "Mesh" or "Vector")
            {
                detection.IsSelected = true;
            }
        }

        BuildKnownFieldsFromDetections();
        var candidateFieldCount = DetectedFields.Count;

        // The raw detection list can be very large for exported playable JSON files.
        // The visible workflow only needs the compact KnownFieldRows built above, so
        // release the hidden candidate cache after each scan to keep memory lower.
        DetectedFields.Clear();

        _loadedPlayablePath = path;
        _loadedPlayableActorClass = ExtractPlayableActorClassName(root, path);
        _loadedPlayableSourceTag = ResolvePlayableSourceTag(_loadedPlayableActorClass, path);
        var scanMode = usedStrictPlayableScan ? "strict playable component scan" : "registry fallback scan";
        StatusText = $"Scanned {Path.GetFileName(path)} using {scanMode}. Found {KnownFieldRows.Count} known editable part(s) from {candidateFieldCount} candidate field(s).";
        _discordPresence.SetActivity("Scanning playable JSON", Path.GetFileName(path));
        FilteredDetections.Refresh();
        FilteredKnownFields.Refresh();
        GC.Collect(2, GCCollectionMode.Optimized, blocking: false, compacting: false);
    }

    private bool CurrentSuitConfigMatchesPlayable(JsonElement playableRoot, string playablePath, out string message)
    {
        return PlayableMatchesSource(SourceTag, SourceActorClassContains, playableRoot, playablePath, out message);
    }

    private bool SuitConfigMatchesPlayable(JsonElement suitRoot, JsonElement playableRoot, string playablePath, out string message)
    {
        var configSourceTag = GetSuitConfigSourceTag(suitRoot);
        var configActorClass = GetSuitConfigActorClassContains(suitRoot);
        return PlayableMatchesSource(configSourceTag, configActorClass, playableRoot, playablePath, out message);
    }

    private bool SuitConfigMatchesLoadedPlayable(JsonElement suitRoot, out string message)
    {
        var configSourceTag = GetSuitConfigSourceTag(suitRoot);
        var configActorClass = GetSuitConfigActorClassContains(suitRoot);
        return LoadedPlayableMatchesSource(configSourceTag, configActorClass, out message);
    }

    private bool LoadedPlayableMatchesSource(string? configSourceTag, string? configActorClass, out string message)
    {
        var playableName = string.IsNullOrWhiteSpace(_loadedPlayablePath) ? "the loaded playable JSON" : Path.GetFileName(_loadedPlayablePath);

        if (!string.IsNullOrWhiteSpace(configSourceTag)
            && !string.IsNullOrWhiteSpace(_loadedPlayableSourceTag)
            && string.Equals(configSourceTag.Trim(), _loadedPlayableSourceTag.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            message = "Matched by source_tag.";
            return true;
        }

        if (!string.IsNullOrWhiteSpace(configActorClass)
            && !string.IsNullOrWhiteSpace(_loadedPlayableActorClass)
            && ActorClassMatches(configActorClass, _loadedPlayableActorClass))
        {
            message = "Matched by source_actor_class_contains.";
            return true;
        }

        message = $"The suit.json target does not match {playableName}.\n\n" +
                  $"suit.json source_tag: {EmptyMarker(configSourceTag)}\n" +
                  $"suit.json source_actor_class_contains: {EmptyMarker(configActorClass)}\n" +
                  $"playable tag: {EmptyMarker(_loadedPlayableSourceTag)}\n" +
                  $"playable actor class: {EmptyMarker(_loadedPlayableActorClass)}\n\n" +
                  "Load the playable JSON exported from the same Batman suit that the suit.json uses as its source/target.";
        return false;
    }

    private bool PlayableMatchesSource(string? configSourceTag, string? configActorClass, JsonElement playableRoot, string playablePath, out string message)
    {
        var playableActorClass = ExtractPlayableActorClassName(playableRoot, playablePath);
        var playableSourceTag = ResolvePlayableSourceTag(playableActorClass, playablePath);

        if (!string.IsNullOrWhiteSpace(configSourceTag)
            && !string.IsNullOrWhiteSpace(playableSourceTag)
            && string.Equals(configSourceTag.Trim(), playableSourceTag.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            message = "Matched by source_tag.";
            return true;
        }

        if (!string.IsNullOrWhiteSpace(configActorClass)
            && !string.IsNullOrWhiteSpace(playableActorClass)
            && ActorClassMatches(configActorClass, playableActorClass))
        {
            message = "Matched by source_actor_class_contains.";
            return true;
        }

        // If the suit only has a tag, use the registry to resolve its expected actor class.
        if (!string.IsNullOrWhiteSpace(configSourceTag) && !string.IsNullOrWhiteSpace(playableActorClass))
        {
            var preset = _registry.PawnPresets.FirstOrDefault(candidate =>
                string.Equals(candidate.Tag, configSourceTag.Trim(), StringComparison.OrdinalIgnoreCase));

            if (preset is not null && ActorClassMatches(preset.ActorClassContains, playableActorClass))
            {
                message = "Matched by registry actor class.";
                return true;
            }
        }

        message = $"The suit.json target does not match {Path.GetFileName(playablePath)}.\n\n" +
                  $"suit.json source_tag: {EmptyMarker(configSourceTag)}\n" +
                  $"suit.json source_actor_class_contains: {EmptyMarker(configActorClass)}\n" +
                  $"playable tag: {EmptyMarker(playableSourceTag)}\n" +
                  $"playable actor class: {EmptyMarker(playableActorClass)}\n\n" +
                  "Open the playable JSON exported from the same Batman suit that the suit.json uses as its source/target.";
        return false;
    }

    private string? GetSuitConfigSourceTag(JsonElement root)
    {
        var direct = GetString(root, "source_tag");
        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        if (root.TryGetProperty("suit", out var suit) && suit.ValueKind == JsonValueKind.Object)
        {
            return GetString(suit, "source_tag")
                   ?? GetString(suit, "copy_from")
                   ?? GetString(suit, "target_pawn");
        }

        return null;
    }

    private string? GetSuitConfigActorClassContains(JsonElement root)
    {
        var direct = GetString(root, "source_actor_class_contains");
        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        if (root.TryGetProperty("suit", out var suit) && suit.ValueKind == JsonValueKind.Object)
        {
            return GetString(suit, "source_actor_class_contains");
        }

        return null;
    }

    private string? ExtractPlayableActorClassName(JsonElement root, string path)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var type = GetJsonString(item, "Type");
                var name = GetJsonString(item, "Name");
                if (type.Equals("BlueprintGeneratedClass", StringComparison.OrdinalIgnoreCase)
                    && LooksLikePlayableActorClassName(name))
                {
                    return name;
                }
            }

            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = GetJsonString(item, "Name");
                if (LooksLikePlayableActorClassName(name))
                {
                    return name;
                }

                var type = GetJsonString(item, "Type");
                if (LooksLikePlayableActorClassName(type))
                {
                    return type;
                }
            }
        }

        var fileBase = Path.GetFileNameWithoutExtension(path);
        if (!string.IsNullOrWhiteSpace(fileBase))
        {
            var candidate = fileBase.EndsWith("_C", StringComparison.OrdinalIgnoreCase) ? fileBase : fileBase + "_C";
            if (LooksLikePlayableActorClassName(candidate))
            {
                return candidate;
            }
        }

        var rawText = root.GetRawText();
        var match = Regex.Match(rawText, @"BP_Batman_[A-Za-z0-9_]+_Playable_C", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : null;
    }

    private string? ResolvePlayableSourceTag(string? playableActorClass, string path)
    {
        if (!string.IsNullOrWhiteSpace(playableActorClass))
        {
            var preset = _registry.PawnPresets.FirstOrDefault(candidate =>
                ActorClassMatches(candidate.ActorClassContains, playableActorClass));

            if (preset is not null)
            {
                return preset.Tag;
            }
        }

        var fileBase = Path.GetFileNameWithoutExtension(path);
        if (!string.IsNullOrWhiteSpace(fileBase))
        {
            var preset = _registry.PawnPresets.FirstOrDefault(candidate =>
                ActorClassMatches(candidate.ActorClassContains, fileBase) ||
                fileBase.Contains(candidate.Name, StringComparison.OrdinalIgnoreCase));

            if (preset is not null)
            {
                return preset.Tag;
            }
        }

        return null;
    }

    private static bool LooksLikePlayableActorClassName(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.Contains("BP_Batman_", StringComparison.OrdinalIgnoreCase)
               && value.Contains("Playable", StringComparison.OrdinalIgnoreCase)
               && value.EndsWith("_C", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ActorClassMatches(string? expected, string? actual)
    {
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
        {
            return false;
        }

        var expectedClean = expected.Trim();
        var actualClean = actual.Trim();
        return actualClean.Equals(expectedClean, StringComparison.OrdinalIgnoreCase)
               || actualClean.Contains(expectedClean, StringComparison.OrdinalIgnoreCase)
               || expectedClean.Contains(actualClean, StringComparison.OrdinalIgnoreCase)
               || actualClean.TrimEnd('_', 'C').Equals(expectedClean.TrimEnd('_', 'C'), StringComparison.OrdinalIgnoreCase);
    }

    private static string EmptyMarker(string? value) => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

    private bool TryScanPlayableExportKnownFields(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var componentCount = 0;
        var index = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in root.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                index++;
                continue;
            }

            var type = GetJsonString(item, "Type");
            var name = GetJsonString(item, "Name");
            var component = GuessPlayableComponentName(item, name);

            if (string.IsNullOrWhiteSpace(component)
                || !_registry.KnownComponents.Contains(component, StringComparer.OrdinalIgnoreCase)
                || !LooksLikeRenderableComponentRecord(item, type, name, component))
            {
                index++;
                continue;
            }

            componentCount++;
            var basePath = $"$[{index}]";
            var componentClass = string.IsNullOrWhiteSpace(type) ? GetDefaultComponentClass(component) : type;

            AddPlayableDetection(
                seen,
                basePath,
                "Component",
                component,
                component,
                "",
                $"{component} component",
                "");

            if (!item.TryGetProperty("Properties", out var properties) ||
                properties.ValueKind != JsonValueKind.Object)
            {
                index++;
                continue;
            }

            var meshPath = AssetObjectPathFromNamedProperty(properties, "SkeletalMesh");
            if (string.IsNullOrWhiteSpace(meshPath))
            {
                meshPath = AssetObjectPathFromNamedProperty(properties, "StaticMesh");
            }
            if (string.IsNullOrWhiteSpace(meshPath))
            {
                meshPath = AssetObjectPathFromNamedProperty(properties, "SkinnedAsset");
            }

            if (!string.IsNullOrWhiteSpace(meshPath))
            {
                AddPlayableDetection(
                    seen,
                    basePath + ".Properties.SkeletalMesh",
                    "Mesh",
                    component,
                    meshPath,
                    "",
                    $"{component} mesh",
                    componentClass);
            }

            if (properties.TryGetProperty("OverrideMaterials", out var materials) &&
                materials.ValueKind == JsonValueKind.Array)
            {
                var slot = 0;
                foreach (var material in materials.EnumerateArray())
                {
                    var materialPath = AssetObjectPathFromObjectRef(material);
                    if (!string.IsNullOrWhiteSpace(materialPath))
                    {
                        AddPlayableDetection(
                            seen,
                            basePath + $".Properties.OverrideMaterials[{slot}]",
                            "Material",
                            component,
                            materialPath,
                            "",
                            $"{component} material slot {slot}",
                            componentClass);
                    }

                    slot++;
                }
            }

            // Some exports expose material parameter arrays on material-like
            // records. These are not common on the playable BP itself, but this
            // keeps vector/scalar-friendly files discoverable without falling
            // back to the noisy whole-file scan.
            if (properties.TryGetProperty("TextureParameterValues", out var textureParams) &&
                textureParams.ValueKind == JsonValueKind.Array)
            {
                AddParameterArrayDetections(textureParams, seen, basePath + ".Properties.TextureParameterValues", component, "Texture Param", componentClass);
            }

            if (properties.TryGetProperty("VectorParameterValues", out var vectorParams) &&
                vectorParams.ValueKind == JsonValueKind.Array)
            {
                AddParameterArrayDetections(vectorParams, seen, basePath + ".Properties.VectorParameterValues", component, "Vector", componentClass);
            }

            if (properties.TryGetProperty("ScalarParameterValues", out var scalarParams) &&
                scalarParams.ValueKind == JsonValueKind.Array)
            {
                AddParameterArrayDetections(scalarParams, seen, basePath + ".Properties.ScalarParameterValues", component, "Scalar", componentClass);
            }

            index++;
        }

        return componentCount > 0;
    }

    private void AddParameterArrayDetections(
        JsonElement parameterArray,
        HashSet<string> seen,
        string basePath,
        string component,
        string kind,
        string componentClass)
    {
        var index = 0;
        foreach (var parameterEntry in parameterArray.EnumerateArray())
        {
            var parameterName = ExtractParameterName(parameterEntry);
            if (!string.IsNullOrWhiteSpace(parameterName))
            {
                AddPlayableDetection(
                    seen,
                    basePath + $"[{index}]",
                    kind,
                    component,
                    parameterName,
                    parameterName,
                    $"{component}.{parameterName}",
                    componentClass);
            }
            index++;
        }
    }

    private void AddPlayableDetection(
        HashSet<string> seen,
        string path,
        string kind,
        string component,
        string currentValue,
        string parameterName,
        string label,
        string componentClass)
    {
        var key = $"{component}|{kind}|{parameterName}|{currentValue}|{path}";
        if (!seen.Add(key))
        {
            return;
        }

        DetectedFields.Add(new DetectedField
        {
            IsSelected = kind is "Texture" or "Texture Param" or "Material" or "Mesh" or "Vector" or "Scalar",
            Kind = kind,
            Label = label,
            SuggestedOperationType = KindToOperationType(kind),
            Component = component,
            ComponentClass = string.IsNullOrWhiteSpace(componentClass) ? GetDefaultComponentClass(component) : componentClass,
            ParameterName = parameterName,
            JsonPath = path,
            CurrentValue = currentValue
        });
    }

    private bool LooksLikeRenderableComponentRecord(JsonElement item, string type, string name, string component)
    {
        if (component.Equals("CharacterMesh0", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var combined = $"{type} {name}";
        if (combined.Contains("MeshComponent", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("SkeletalMeshComponent", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("StaticMeshComponent", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!item.TryGetProperty("Properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return properties.TryGetProperty("SkeletalMesh", out _)
               || properties.TryGetProperty("StaticMesh", out _)
               || properties.TryGetProperty("SkinnedAsset", out _)
               || properties.TryGetProperty("OverrideMaterials", out _);
    }

    private string GuessPlayableComponentName(JsonElement item, string name)
    {
        if (_registry.KnownComponents.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            return name;
        }

        const string genSuffix = "_GEN_VARIABLE";
        if (name.EndsWith(genSuffix, StringComparison.OrdinalIgnoreCase))
        {
            var stripped = name[..^genSuffix.Length];
            if (_registry.KnownComponents.Contains(stripped, StringComparer.OrdinalIgnoreCase))
            {
                return stripped;
            }
        }

        if (item.TryGetProperty("Properties", out var properties) &&
            properties.ValueKind == JsonValueKind.Object)
        {
            if (properties.TryGetProperty("ComponentTags", out var tags) &&
                tags.ValueKind == JsonValueKind.Array)
            {
                foreach (var tag in tags.EnumerateArray())
                {
                    var text = tag.ValueKind == JsonValueKind.String ? tag.GetString() ?? "" : tag.ToString();
                    foreach (var component in _registry.KnownComponents)
                    {
                        if (text.Equals(component, StringComparison.OrdinalIgnoreCase)
                            || text.EndsWith("." + component, StringComparison.OrdinalIgnoreCase))
                        {
                            return component;
                        }
                    }
                }
            }
        }

        return "";
    }

    private static string GetJsonString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
    }

    private static string AssetObjectPathFromNamedProperty(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return AssetObjectPathFromObjectRef(value);
    }

    private static string AssetObjectPathFromObjectRef(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return NormalizeExportedAssetPath(value.GetString() ?? "");
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return "";
        }

        var objectPath = GetJsonString(value, "ObjectPath");
        if (!string.IsNullOrWhiteSpace(objectPath))
        {
            return NormalizeExportedAssetPath(objectPath);
        }

        return NormalizeExportedAssetPath(GetJsonString(value, "ObjectName"));
    }

    private static string NormalizeExportedAssetPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "";
        }

        var value = path.Trim();

        var quoteIndex = value.LastIndexOf('\'');
        if (quoteIndex >= 0 && quoteIndex + 1 < value.Length)
        {
            value = value[(quoteIndex + 1)..];
        }

        value = value.Trim('\'', '"');

        if (!value.StartsWith("/Game/", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var slash = value.LastIndexOf('/');
        var dot = value.LastIndexOf('.');
        if (dot > slash && dot + 1 < value.Length)
        {
            var suffix = value[(dot + 1)..];
            if (suffix.All(char.IsDigit))
            {
                var leaf = value[(slash + 1)..dot];
                return value[..dot] + "." + leaf;
            }
        }

        if (dot <= slash && slash >= 0 && slash + 1 < value.Length)
        {
            var leaf = value[(slash + 1)..];
            return value + "." + leaf;
        }

        return value;
    }

    private static string ExtractParameterName(JsonElement parameterEntry)
    {
        if (parameterEntry.ValueKind != JsonValueKind.Object)
        {
            return "";
        }

        var directName = GetJsonString(parameterEntry, "ParameterName");
        if (!string.IsNullOrWhiteSpace(directName))
        {
            return directName;
        }

        if (parameterEntry.TryGetProperty("ParameterInfo", out var info) &&
            info.ValueKind == JsonValueKind.Object)
        {
            return GetJsonString(info, "Name");
        }

        return "";
    }

    private void TraverseForDetections(JsonElement element, string path, string name, HashSet<string> seen)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    TraverseForDetections(prop.Value, JoinPath(path, prop.Name), prop.Name, seen);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    TraverseForDetections(item, $"{path}[{index}]", name, seen);
                    index++;
                }
                break;

            case JsonValueKind.String:
                ConsiderDetection(path, name, element.GetString() ?? "", seen);
                break;

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                ConsiderDetection(path, name, element.ToString(), seen);
                break;
        }
    }

    private void ConsiderDetection(string path, string name, string value, HashSet<string> seen)
    {
        var valueText = value ?? "";
        if (string.IsNullOrWhiteSpace(valueText) && !_registry.KnownComponents.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var pathOrValue = $"{path} {name} {valueText}";
        var matchesRegistry = _registry.ScanKeywords.Any(k => pathOrValue.Contains(k, StringComparison.OrdinalIgnoreCase))
                              || _registry.KnownComponents.Any(k => pathOrValue.Contains(k, StringComparison.OrdinalIgnoreCase))
                              || _registry.TextureParameters.Any(k => string.Equals(valueText, k, StringComparison.OrdinalIgnoreCase))
                              || _registry.VectorParameters.Any(k => pathOrValue.Contains(k, StringComparison.OrdinalIgnoreCase))
                              || _registry.ScalarParameters.Any(k => pathOrValue.Contains(k, StringComparison.OrdinalIgnoreCase));

        if (!matchesRegistry)
        {
            return;
        }

        var currentValueLooksUseful = valueText.StartsWith("/Game/", StringComparison.OrdinalIgnoreCase)
                                      || valueText.Contains("Pawns.Playable.Batman", StringComparison.OrdinalIgnoreCase)
                                      || _registry.KnownComponents.Contains(valueText, StringComparer.OrdinalIgnoreCase)
                                      || _registry.TextureParameters.Contains(valueText, StringComparer.OrdinalIgnoreCase)
                                      || _registry.VectorParameters.Contains(valueText, StringComparer.OrdinalIgnoreCase)
                                      || _registry.ScalarParameters.Contains(valueText, StringComparer.OrdinalIgnoreCase)
                                      || name.Contains("Mesh", StringComparison.OrdinalIgnoreCase)
                                      || name.Contains("Material", StringComparison.OrdinalIgnoreCase)
                                      || name.Contains("Equipment", StringComparison.OrdinalIgnoreCase);

        if (!currentValueLooksUseful)
        {
            return;
        }

        var key = $"{path}|{valueText}";
        if (!seen.Add(key))
        {
            return;
        }

        var kind = GuessKind(path, name, valueText);
        var component = GuessComponent(path, valueText);
        var parameter = kind switch
        {
            "Vector" => GuessVectorParameter(path, name, valueText),
            "Scalar" => GuessScalarParameter(path, name, valueText),
            _ => GuessTextureParameter(path, valueText)
        };
        var opType = KindToOperationType(kind);
        var label = BuildDetectionLabel(kind, component, parameter, name);

        DetectedFields.Add(new DetectedField
        {
            IsSelected = false,
            Kind = kind,
            Label = label,
            SuggestedOperationType = opType,
            Component = component,
            ParameterName = parameter,
            JsonPath = path,
            CurrentValue = valueText
        });
    }

    private string GuessKind(string path, string name, string value)
    {
        var combined = $"{path} {name} {value}";

        if (combined.Contains("Equipment", StringComparison.OrdinalIgnoreCase) || value.Contains("DA_ETA_", StringComparison.OrdinalIgnoreCase))
        {
            return "Equipment";
        }

        if (_registry.VectorParameters.Contains(value, StringComparer.OrdinalIgnoreCase)
            || _registry.VectorParameters.Contains(name, StringComparer.OrdinalIgnoreCase)
            || combined.Contains("VectorParameterValues", StringComparison.OrdinalIgnoreCase))
        {
            return "Vector";
        }

        if (_registry.ScalarParameters.Contains(value, StringComparer.OrdinalIgnoreCase)
            || _registry.ScalarParameters.Contains(name, StringComparer.OrdinalIgnoreCase)
            || combined.Contains("ScalarParameterValues", StringComparison.OrdinalIgnoreCase))
        {
            return "Scalar";
        }

        if (_registry.TextureParameters.Contains(value, StringComparer.OrdinalIgnoreCase)
            || combined.Contains("TextureParameterValues", StringComparison.OrdinalIgnoreCase))
        {
            return value.StartsWith("/Game/", StringComparison.OrdinalIgnoreCase) ? "Texture" : "Texture Param";
        }

        if (value.Contains("/T_", StringComparison.OrdinalIgnoreCase) || value.Contains("Texture", StringComparison.OrdinalIgnoreCase))
        {
            return "Texture";
        }

        if (value.Contains("/MI_", StringComparison.OrdinalIgnoreCase) || combined.Contains("Material", StringComparison.OrdinalIgnoreCase))
        {
            return "Material";
        }

        if (value.Contains("/SK_", StringComparison.OrdinalIgnoreCase) || value.Contains("/SM_", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("SkeletalMesh", StringComparison.OrdinalIgnoreCase) || combined.Contains("StaticMesh", StringComparison.OrdinalIgnoreCase))
        {
            return "Mesh";
        }

        if (_registry.KnownComponents.Contains(value, StringComparer.OrdinalIgnoreCase) || _registry.KnownComponents.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            return "Component";
        }

        if (value.Contains("Pawns.Playable.Batman", StringComparison.OrdinalIgnoreCase))
        {
            return "Pawn Tag";
        }

        return "Known Field";
    }

    private string GuessComponent(string path, string value)
    {
        foreach (var component in _registry.KnownComponents)
        {
            if (path.Contains(component, StringComparison.OrdinalIgnoreCase) || value.Contains(component, StringComparison.OrdinalIgnoreCase))
            {
                return component;
            }
        }

        return "CharacterMesh0";
    }

    private string GuessTextureParameter(string path, string value)
    {
        foreach (var parameter in _registry.TextureParameters)
        {
            if (string.Equals(value, parameter, StringComparison.OrdinalIgnoreCase) || path.Contains(parameter, StringComparison.OrdinalIgnoreCase))
            {
                return parameter;
            }
        }

        return "BC";
    }

    private string GuessVectorParameter(string path, string name, string value)
    {
        var combined = $"{path} {name} {value}";
        foreach (var parameter in _registry.VectorParameters)
        {
            if (combined.Contains(parameter, StringComparison.OrdinalIgnoreCase))
            {
                return parameter;
            }
        }

        return VectorParameterChoices.FirstOrDefault() ?? "Base Color";
    }

    private string GuessScalarParameter(string path, string name, string value)
    {
        var combined = $"{path} {name} {value}";
        foreach (var parameter in _registry.ScalarParameters)
        {
            if (combined.Contains(parameter, StringComparison.OrdinalIgnoreCase))
            {
                return parameter;
            }
        }

        return ScalarParameterChoices.FirstOrDefault() ?? "ColourChannel_Picker";
    }

    private static string KindToOperationType(string kind) => kind switch
    {
        "Texture" or "Texture Param" => "set_texture_parameter",
        "Vector" => "set_vector_parameter",
        "Scalar" => "set_scalar_parameter",
        "Material" => "set_material",
        "Mesh" => "set_static_mesh",
        _ => "set_texture_parameter"
    };

    private static string BuildDetectionLabel(string kind, string component, string parameter, string name)
    {
        return kind switch
        {
            "Texture" or "Texture Param" => $"{component}.{parameter}",
            "Vector" => $"{component}.{parameter} vector",
            "Scalar" => $"{component}.{parameter} scalar",
            "Material" => $"{component} material",
            "Mesh" => $"{component} mesh",
            _ => string.IsNullOrWhiteSpace(name) ? kind : name
        };
    }

    private void BuildKnownFieldsFromDetections()
    {
        KnownFieldRows.Clear();

        var groups = DetectedFields
            .GroupBy(d => d.Kind == "Equipment" ? $"Equipment|{d.CurrentValue}" : d.Component, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in groups)
        {
            var first = group.First();
            var isEquipment = group.Any(d => d.Kind == "Equipment");
            var component = isEquipment ? "Equipment" : first.Component;
            var kinds = group.Select(d => d.Kind).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Do not let the component-name marker itself become the main
            // value. For a part card, the useful summary is the mesh/material
            // currently on that part. This makes CharacterMesh0 show its real
            // body material, e.g. MI_Batman_66_EoM, instead of a stray parent
            // reference from another SCS node.
            var displayValues = group
                .Where(d => !d.Kind.Equals("Component", StringComparison.OrdinalIgnoreCase))
                .Select(d => d.CurrentValue)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList();

            var primaryValue = group.FirstOrDefault(d => d.Kind.Equals("Material", StringComparison.OrdinalIgnoreCase))?.CurrentValue
                               ?? group.FirstOrDefault(d => d.Kind.Equals("Mesh", StringComparison.OrdinalIgnoreCase))?.CurrentValue
                               ?? displayValues.FirstOrDefault()
                               ?? first.CurrentValue;

            var materialDetails = BuildMaterialSlotDetails(group);
            var componentClass = group.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.ComponentClass))?.ComponentClass;

            KnownFieldRows.Add(new KnownEditableField
            {
                Component = component,
                ComponentClass = isEquipment ? "DataAsset" : (string.IsNullOrWhiteSpace(componentClass) ? GetDefaultComponentClass(component) : componentClass),
                ParameterName = group.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.ParameterName))?.ParameterName ?? "BC",
                PrimaryValue = primaryValue,
                JsonPath = first.JsonPath,
                DetectedKinds = string.Join(", ", kinds),
                Summary = displayValues.Count == 0 ? first.Label : string.Join("  |  ", displayValues.Select(v => Shorten(v, 90))),
                MaterialDetails = materialDetails,
                AvailableOperations = BuildAvailableOperationsText(kinds, component)
            });
        }

        SelectedKnownField = KnownFieldRows.FirstOrDefault();
    }

    private static string BuildMaterialSlotDetails(IEnumerable<DetectedField> fields)
    {
        var materialRows = fields
            .Where(d => d.Kind.Equals("Material", StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(d.CurrentValue))
            .Select(d => new
            {
                Slot = ExtractMaterialSlot(d),
                Value = d.CurrentValue.Trim()
            })
            .OrderBy(d => d.Slot)
            .ThenBy(d => d.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (materialRows.Count == 0)
        {
            return string.Empty;
        }

        return "MATERIAL SLOTS" + Environment.NewLine
               + string.Join(Environment.NewLine, materialRows.Select(row => $"[{row.Slot}]  {row.Value}"));
    }

    private static int ExtractMaterialSlot(DetectedField field)
    {
        var fromPath = Regex.Match(field.JsonPath ?? string.Empty, @"OverrideMaterials\[(\d+)\]", RegexOptions.IgnoreCase);
        if (fromPath.Success && int.TryParse(fromPath.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pathSlot))
        {
            return pathSlot;
        }

        var fromLabel = Regex.Match(field.Label ?? string.Empty, @"slot\s+(\d+)", RegexOptions.IgnoreCase);
        if (fromLabel.Success && int.TryParse(fromLabel.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var labelSlot))
        {
            return labelSlot;
        }

        return 0;
    }

    private void BuildKnownFieldsFromOperations()
    {
        KnownFieldRows.Clear();

        var groups = OperationRows
            .Where(o => !string.IsNullOrWhiteSpace(o.Component))
            .GroupBy(o => o.Component, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var first = group.First();
            var kinds = group.Select(o => OperationTypeToKind(o.Type)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var values = group.Select(o => o.Asset).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).Take(4).ToList();

            KnownFieldRows.Add(new KnownEditableField
            {
                Component = first.Component,
                ComponentClass = string.IsNullOrWhiteSpace(first.ComponentClass) ? GetDefaultComponentClass(first.Component) : first.ComponentClass,
                ParameterName = string.IsNullOrWhiteSpace(first.ParameterName) ? "BC" : first.ParameterName,
                PrimaryValue = values.FirstOrDefault() ?? "",
                JsonPath = "Imported from suit.json operations",
                DetectedKinds = string.Join(", ", kinds),
                Summary = values.Count == 0 ? "Existing generated operation" : string.Join("  |  ", values.Select(v => Shorten(v, 90))),
                AvailableOperations = BuildAvailableOperationsText(kinds, first.Component)
            });
        }

        if (EquipmentRows.Count > 0)
        {
            KnownFieldRows.Add(new KnownEditableField
            {
                Component = "Equipment",
                ComponentClass = "DataAsset",
                ParameterName = "",
                PrimaryValue = EquipmentRows.First().ReplaceEquipment,
                JsonPath = "Imported from suit.json equipment_replacements",
                DetectedKinds = "Equipment",
                Summary = string.Join("  |  ", EquipmentRows.Select(e => Shorten(e.ReplaceEquipment, 90)).Where(v => !string.IsNullOrWhiteSpace(v)).Take(4)),
                AvailableOperations = "Equipment Replacement"
            });
        }

        SelectedKnownField = KnownFieldRows.FirstOrDefault();
    }

    private string BuildAvailableOperationsText(IEnumerable<string> kinds, string component)
    {
        var kindList = kinds.ToList();
        var actions = new List<string>();

        if (kindList.Any(k => k.Contains("Texture", StringComparison.OrdinalIgnoreCase)))
        {
            actions.Add("Texture Param");
        }

        if (kindList.Any(k => k.Contains("Vector", StringComparison.OrdinalIgnoreCase)
                              || k.Contains("Material", StringComparison.OrdinalIgnoreCase)
                              || k.Contains("Texture", StringComparison.OrdinalIgnoreCase)))
        {
            actions.Add("Vector Color");
        }

        if (kindList.Any(k => k.Contains("Material", StringComparison.OrdinalIgnoreCase)))
        {
            actions.Add("Material");
        }

        if (kindList.Any(k => k.Contains("Mesh", StringComparison.OrdinalIgnoreCase)))
        {
            actions.Add("Mesh");
        }

        if (kindList.Any(k => k.Contains("Equipment", StringComparison.OrdinalIgnoreCase)) || component.Equals("Equipment", StringComparison.OrdinalIgnoreCase))
        {
            actions.Add("Equipment Replacement");
        }
        else
        {
            actions.Add("Visibility");
            actions.Add("Hidden In Game");
            actions.Add("Attach");
            actions.Add("Ensure Component");
        }

        return string.Join("  •  ", actions.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string OperationTypeToKind(string type) => type switch
    {
        "set_texture_parameter" => "Texture",
        "set_vector_parameter" => "Vector",
        "set_scalar_parameter" => "Scalar",
        "set_material" => "Material",
        "set_static_mesh" => "Mesh",
        "set_visibility" => "Visibility",
        "set_hidden_in_game" => "Hidden",
        "attach_component" => "Attach",
        "ensure_component" => "Component",
        _ => "Operation"
    };

    private void AddOperationFromKnownField(string type)
    {
        if (SelectedKnownField is null)
        {
            WinMessageBox.Show(this, "Load a playable JSON and select a known editable field first.", "No known field selected", WinMessageBoxButton.OK, WinMessageBoxImage.Information);
            return;
        }

        if (SelectedKnownField.Component.Equals("Equipment", StringComparison.OrdinalIgnoreCase))
        {
            AddEquipmentFromKnownField();
            return;
        }

        var row = CreateOperationRow(type, SelectedKnownField.Component, SelectedKnownField.ParameterName);
        row.Component = SelectedKnownField.Component;
        row.ComponentClass = string.IsNullOrWhiteSpace(SelectedKnownField.ComponentClass) ? GetDefaultComponentClass(SelectedKnownField.Component) : SelectedKnownField.ComponentClass;
        row.ParameterName = string.IsNullOrWhiteSpace(SelectedKnownField.ParameterName) ? "BC" : SelectedKnownField.ParameterName;
        if (type == "set_vector_parameter")
        {
            row.ParameterName = VectorParameterChoices.Contains(row.ParameterName, StringComparer.OrdinalIgnoreCase)
                ? row.ParameterName
                : VectorParameterChoices.FirstOrDefault() ?? "Base Color";
            row.Asset = "1.0, 1.0, 1.0, 1.0";
        }
        else if (type == "set_scalar_parameter")
        {
            row.ParameterName = ScalarParameterChoices.Contains(row.ParameterName, StringComparer.OrdinalIgnoreCase)
                ? row.ParameterName
                : ScalarParameterChoices.FirstOrDefault() ?? "ColourChannel_Picker";
            row.Asset = "0.0";
        }
        row.Notes = $"Generated from {SelectedKnownField.Component}. Current: {Shorten(SelectedKnownField.PrimaryValue, 140)}";

        if (type is "attach_component" or "ensure_component")
        {
            row.Parent = string.IsNullOrWhiteSpace(row.Parent) ? "CharacterMesh0" : row.Parent;
        }

        OperationRows.Add(row);
        SelectedOperation = row;
        FilteredOperations.Refresh();
        StatusText = $"Added {type} operation for {SelectedKnownField.Component}. Fill in Asset / Value before exporting.";
    }

    private void AddEquipmentFromKnownField()
    {
        if (SelectedKnownField is null)
        {
            WinMessageBox.Show(this, "Select a known field first.", "No known field selected", WinMessageBoxButton.OK, WinMessageBoxImage.Information);
            return;
        }

        EquipmentRows.Add(new EquipmentRow
        {
            Enabled = true,
            Slot = "0",
            ReplaceEquipment = SelectedKnownField.PrimaryValue,
            WithEquipment = "",
            Notes = $"Generated from known field: {SelectedKnownField.Summary}"
        });

        StatusText = "Added equipment replacement row. Fill in With Equipment on page 4 before exporting.";
    }

    private void AddOperation(string type)
    {
        OperationRows.Add(CreateOperationRow(type, "CharacterMesh0", "BC"));
        StatusText = $"Added {type} operation.";
    }

    private OperationRow CreateOperationRow(string type, string component, string parameterName)
    {
        var row = new OperationRow
        {
            Enabled = true,
            Type = type,
            Component = string.IsNullOrWhiteSpace(component) ? "CharacterMesh0" : component,
            ComponentClass = GetDefaultComponentClass(component),
            MaterialSlot = "0",
            ParameterName = string.IsNullOrWhiteSpace(parameterName) ? "BC" : parameterName,
            ApplyTo = "both",
            Required = false,
            Parent = "CharacterMesh0",
            Socket = "",
            Asset = "",
            Notes = ""
        };

        if (type == "set_vector_parameter")
        {
            row.ParameterName = VectorParameterChoices.FirstOrDefault() ?? "Base Color";
            row.Asset = "1.0, 1.0, 1.0, 1.0";
        }
        else if (type == "set_scalar_parameter")
        {
            row.ParameterName = ScalarParameterChoices.FirstOrDefault() ?? "ColourChannel_Picker";
            row.Asset = "0.0";
        }
        else if (type == "set_visibility")
        {
            row.Asset = "true";
        }
        else if (type == "set_hidden_in_game")
        {
            row.Asset = "true";
            row.ApplyTo = "gameplay";
        }
        else if (type == "attach_component")
        {
            row.Component = "NeckPeg";
            row.ComponentClass = "StaticMeshComponent";
            row.Parent = "CharacterMesh0";
            row.Socket = "HeadStud_Attach_Socket";
        }
        else if (type == "ensure_component")
        {
            row.Component = "Torso2";
            row.ComponentClass = "SkeletalMeshComponentBudgeted";
            row.Parent = "CharacterMesh0";
            row.Socket = "Chest_Socket";
            row.ReinitializePose = true;
        }

        return row;
    }

    private Dictionary<string, object?> BuildOperationDictionary(OperationRow row)
    {
        var d = new Dictionary<string, object?>
        {
            ["type"] = row.Type
        };

        AddIfNotBlank(d, "component", row.Component);
        AddIfNotBlank(d, "component_class", row.ComponentClass);

        switch (row.Type)
        {
            case "set_texture_parameter":
                AddInt(d, "material_slot", row.MaterialSlot, 0);
                AddIfNotBlank(d, "parameter_name", row.ParameterName);
                AddIfNotBlank(d, "asset", row.Asset);
                AddApplyFlags(d, row);
                break;

            case "set_vector_parameter":
                AddInt(d, "material_slot", row.MaterialSlot, 0);
                AddIfNotBlank(d, "parameter_name", row.ParameterName);
                d["value"] = ParseVectorValue(row.Asset);
                AddApplyFlags(d, row);
                break;

            case "set_scalar_parameter":
                AddInt(d, "material_slot", row.MaterialSlot, 0);
                AddIfNotBlank(d, "parameter_name", row.ParameterName);
                d["value"] = ParseScalarValue(row.Asset);
                AddApplyFlags(d, row);
                break;

            case "set_material":
                AddInt(d, "material_slot", row.MaterialSlot, 0);
                AddIfNotBlank(d, "asset", row.Asset);
                AddApplyFlags(d, row);
                break;

            case "set_static_mesh":
                AddIfNotBlank(d, "asset", row.Asset);
                AddApplyFlags(d, row);
                break;

            case "set_visibility":
                d["visible"] = ParseBool(row.Asset, true);
                d["propagate_to_children"] = row.PropagateToChildren;
                AddApplyFlags(d, row);
                break;

            case "set_hidden_in_game":
                d["hidden"] = ParseBool(row.Asset, true);
                d["propagate_to_children"] = row.PropagateToChildren;
                AddApplyFlags(d, row);
                break;

            case "attach_component":
                AddIfNotBlank(d, "parent", row.Parent);
                AddIfNotBlank(d, "socket", row.Socket);
                AddApplyFlags(d, row);
                break;

            case "ensure_component":
                AddIfNotBlank(d, "parent", row.Parent);
                AddIfNotBlank(d, "socket", row.Socket);
                AddIfNotBlank(d, "asset", row.Asset);
                var materials = SplitCsv(row.MaterialsCsv);
                if (materials.Count > 0)
                {
                    d["materials"] = materials;
                }
                d["visible"] = true;
                d["hidden"] = false;
                d["propagate_to_children"] = row.PropagateToChildren;
                d["reinitialize_pose"] = row.ReinitializePose;
                AddApplyFlags(d, row);
                break;

            default:
                AddIfNotBlank(d, "asset", row.Asset);
                AddApplyFlags(d, row);
                break;
        }

        return d;
    }

    private void AddApplyFlags(Dictionary<string, object?> d, OperationRow row)
    {
        AddIfNotBlank(d, "apply_to", row.ApplyTo);
        d["required"] = row.Required;
        d["enabled"] = true;
    }

    private static void AddIfNotBlank(Dictionary<string, object?> d, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            d[key] = value;
        }
    }

    private static void AddInt(Dictionary<string, object?> d, string key, string value, int defaultValue)
    {
        d[key] = ParseInt(value, defaultValue);
    }

    private static List<string> SplitCsv(string csv)
    {
        return csv.Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static object ParseVectorValue(string text)
    {
        var trimmed = (text ?? string.Empty).Trim();

        static Dictionary<string, double> MakeVectorObject(double r, double g, double b, double a) => new()
        {
            ["r"] = RoundVector(r),
            ["g"] = RoundVector(g),
            ["b"] = RoundVector(b),
            ["a"] = RoundVector(a)
        };

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return MakeVectorObject(1.0, 1.0, 1.0, 1.0);
        }

        if (trimmed.StartsWith("#", StringComparison.Ordinal) && (trimmed.Length == 7 || trimmed.Length == 9))
        {
            try
            {
                var r = Convert.ToInt32(trimmed.Substring(1, 2), 16) / 255.0;
                var g = Convert.ToInt32(trimmed.Substring(3, 2), 16) / 255.0;
                var b = Convert.ToInt32(trimmed.Substring(5, 2), 16) / 255.0;
                var a = trimmed.Length == 9 ? Convert.ToInt32(trimmed.Substring(7, 2), 16) / 255.0 : 1.0;
                return MakeVectorObject(r, g, b, a);
            }
            catch
            {
                return MakeVectorObject(1.0, 1.0, 1.0, 1.0);
            }
        }

        var parts = trimmed.Split([',', ';', ' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var values = new List<double>();
        foreach (var part in parts)
        {
            if (double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                values.Add(value);
            }
            else
            {
                return MakeVectorObject(1.0, 1.0, 1.0, 1.0);
            }
        }

        if (values.Count is 3 or 4)
        {
            if (values.Count == 3)
            {
                values.Add(1.0);
            }

            return MakeVectorObject(values[0], values[1], values[2], values[3]);
        }

        return MakeVectorObject(1.0, 1.0, 1.0, 1.0);
    }

    private static double RoundVector(double value) => Math.Round(value, 6);

    private static double ParseScalarValue(string text)
    {
        return double.TryParse((text ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0.0;
    }

    private static bool OperationNeedsAsset(string type) => type is "set_texture_parameter" or "set_vector_parameter" or "set_scalar_parameter" or "set_material" or "set_static_mesh" or "set_skeletal_mesh" or "create_skeletal_component" or "set_anim_class" or "ensure_component";

    private bool FilterOperation(object obj)
    {
        if (obj is not OperationRow row)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(OperationFilter))
        {
            return true;
        }

        var text = $"{row.Type} {row.Component} {row.ComponentClass} {row.ParameterName} {row.Asset} {row.Parent} {row.Socket} {row.Notes}";
        return text.Contains(OperationFilter, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterKnownField(object obj)
    {
        if (obj is not KnownEditableField field)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(KnownFieldFilter))
        {
            return true;
        }

        var text = $"{field.Component} {field.ComponentClass} {field.ParameterName} {field.DetectedKinds} {field.Summary} {field.AvailableOperations} {field.JsonPath}";
        return text.Contains(KnownFieldFilter, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterDetection(object obj)
    {
        if (obj is not DetectedField field)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(DetectionFilter))
        {
            return true;
        }

        var text = $"{field.Kind} {field.Label} {field.Component} {field.ParameterName} {field.CurrentValue} {field.JsonPath}";
        return text.Contains(DetectionFilter, StringComparison.OrdinalIgnoreCase);
    }

    private void LoadRegistry()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Data");
        EnsureRegistryFileExists(folder);
        var path = Path.Combine(folder, "EditableFieldRegistry.json");

        try
        {
            var json = File.ReadAllText(path);
            _registry = JsonSerializer.Deserialize<EditableFieldRegistry>(json, _jsonOptions) ?? EditableFieldRegistry.Default();
        }
        catch
        {
            _registry = EditableFieldRegistry.Default();
        }

        var mergedRegistry = MergeRegistryWithBuiltInDefaults(_registry);
        _registry = mergedRegistry.Registry;

        if (mergedRegistry.Changed)
        {
            try
            {
                File.WriteAllText(path, JsonSerializer.Serialize(_registry, _jsonOptions));
            }
            catch
            {
                // Not fatal. The in-memory registry is still updated for this run.
            }
        }

        ReplaceCollection(OperationTypeChoices, _registry.OperationTypes);
        ReplaceCollection(ComponentChoices, _registry.KnownComponents);
        ReplaceCollection(ComponentClassChoices, _registry.ComponentClasses);
        ReplaceCollection(TextureParameterChoices, _registry.TextureParameters);
        ReplaceCollection(VectorParameterChoices, _registry.VectorParameters);
        ReplaceCollection(ScalarParameterChoices, _registry.ScalarParameters);
        ReplaceCollection(ApplyToChoices, _registry.ApplyToValues);
        ReplaceCollection(PawnTagPresets, _registry.PawnPresets.Select(p => p.Tag));
        ReplaceCollection(ActorClassPresets, _registry.PawnPresets.Select(p => p.ActorClassContains));

        if (PawnTagPresets.Contains(SourceTag))
        {
            SyncActorClassFromPawnTag(SourceTag);
        }

        StatusText = $"Ready. Loaded registry with {_registry.PawnPresets.Count} pawn presets, {_registry.KnownComponents.Count} components, {_registry.TextureParameters.Count} texture params, {_registry.VectorParameters.Count} vector params, {_registry.ScalarParameters.Count} scalar params, and {_registry.OperationTypes.Count} operation types.";
    }

    private static (EditableFieldRegistry Registry, bool Changed) MergeRegistryWithBuiltInDefaults(EditableFieldRegistry loaded)
    {
        var defaults = EditableFieldRegistry.Default();
        var changed = false;

        static List<string> MergeStringList(List<string> preferred, List<string> extras, ref bool changed)
        {
            var result = new List<string>();

            foreach (var value in preferred.Concat(extras))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (result.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                result.Add(value);
            }

            if (result.Count != extras.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                changed = true;
            }

            return result;
        }

        loaded.Notes = string.IsNullOrWhiteSpace(loaded.Notes) ? defaults.Notes : loaded.Notes;
        loaded.KnownComponents ??= [];
        loaded.ComponentClasses ??= [];
        loaded.ComponentClassDefaults ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        loaded.TextureParameters ??= [];
        loaded.VectorParameters ??= [];
        loaded.OperationTypes ??= [];
        loaded.ApplyToValues ??= [];
        loaded.ScanKeywords ??= [];
        loaded.PawnPresets ??= [];

        loaded.KnownComponents = MergeStringList(defaults.KnownComponents, loaded.KnownComponents, ref changed);
        loaded.ComponentClasses = MergeStringList(defaults.ComponentClasses, loaded.ComponentClasses, ref changed);
        loaded.TextureParameters = MergeStringList(defaults.TextureParameters, loaded.TextureParameters, ref changed);
        loaded.VectorParameters = MergeStringList(defaults.VectorParameters, loaded.VectorParameters, ref changed);
        loaded.OperationTypes = MergeStringList(defaults.OperationTypes, loaded.OperationTypes, ref changed);
        loaded.ApplyToValues = MergeStringList(defaults.ApplyToValues, loaded.ApplyToValues, ref changed);
        loaded.ScanKeywords = MergeStringList(defaults.ScanKeywords, loaded.ScanKeywords, ref changed);

        foreach (var pair in defaults.ComponentClassDefaults)
        {
            if (!loaded.ComponentClassDefaults.ContainsKey(pair.Key))
            {
                loaded.ComponentClassDefaults[pair.Key] = pair.Value;
                changed = true;
            }
        }

        var mergedPresets = new List<PawnPreset>();

        void AddPreset(PawnPreset preset)
        {
            if (string.IsNullOrWhiteSpace(preset.Tag))
            {
                return;
            }

            var existingIndex = mergedPresets.FindIndex(existing => string.Equals(existing.Tag, preset.Tag, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                if (string.IsNullOrWhiteSpace(mergedPresets[existingIndex].ActorClassContains) && !string.IsNullOrWhiteSpace(preset.ActorClassContains))
                {
                    mergedPresets[existingIndex] = preset;
                    changed = true;
                }

                return;
            }

            mergedPresets.Add(preset);
        }

        // Defaults first keeps the Batman playable tag dropdown predictable even
        // if the build output still has an older copied registry file. Any custom
        // presets the user added stay appended after the known built-in list.
        foreach (var preset in defaults.PawnPresets)
        {
            AddPreset(preset);
        }

        foreach (var preset in loaded.PawnPresets)
        {
            AddPreset(preset);
        }

        if (mergedPresets.Count != loaded.PawnPresets.Count ||
            !mergedPresets.Select(p => p.Tag).SequenceEqual(loaded.PawnPresets.Select(p => p.Tag), StringComparer.OrdinalIgnoreCase))
        {
            changed = true;
        }

        loaded.PawnPresets = mergedPresets;

        return (loaded, changed);
    }

    private void SyncActorClassFromPawnTag(string? pawnTag)
    {
        if (string.IsNullOrWhiteSpace(pawnTag))
        {
            return;
        }

        var preset = _registry.PawnPresets.FirstOrDefault(candidate =>
            string.Equals(candidate.Tag, pawnTag, StringComparison.OrdinalIgnoreCase));

        if (preset is not null && !string.IsNullOrWhiteSpace(preset.ActorClassContains))
        {
            SourceActorClassContains = preset.ActorClassContains;
        }
    }

    private void EnsureRegistryFileExists(string folder)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "EditableFieldRegistry.json");
        if (!File.Exists(path))
        {
            File.WriteAllText(path, JsonSerializer.Serialize(EditableFieldRegistry.Default(), _jsonOptions));
        }
    }

    private static void ReplaceCollection(ObservableCollection<string> target, IEnumerable<string> values)
    {
        target.Clear();
        foreach (var value in values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            target.Add(value);
        }
    }

    private string GetDefaultComponentClass(string component)
    {
        if (!string.IsNullOrWhiteSpace(component) && _registry.ComponentClassDefaults.TryGetValue(component, out var cls))
        {
            return cls;
        }

        return _registry.ComponentClasses.FirstOrDefault() ?? "MeshComponent";
    }

    private bool LooksLikeNewSuitSlotConfig(JsonElement root)
    {
        return root.ValueKind == JsonValueKind.Object
               && (root.TryGetProperty("operations", out _) || root.TryGetProperty("equipment_replacements", out _))
               && (root.TryGetProperty("slot_id", out _) || root.TryGetProperty("schema_version", out _));
    }

    private static string JoinPath(string parent, string property)
    {
        return parent == "$" ? $"$.{property}" : $"{parent}.{property}";
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static bool? GetBoolNullable(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => ParseBool(value.GetString(), false),
            _ => null
        };
    }

    private static int? GetIntNullable(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i))
        {
            return i;
        }

        return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private static int ParseInt(string text, int fallback)
    {
        return int.TryParse(text, out var value) ? value : fallback;
    }

    private static bool ParseBool(string? text, bool fallback)
    {
        if (bool.TryParse(text, out var value))
        {
            return value;
        }

        if (string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "no", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return fallback;
    }

    private static bool IsAllowedSlotIdChar(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_' || value == '-';
    }

    private static bool IsBlockedDisplayNameChar(char value)
    {
        return char.IsControl(value)
            || value == '!'
            || value == '/'
            || value == '\\'
            || value == '?'
            || value == ':'
            || value == '*'
            || value == '"'
            || value == '<'
            || value == '>'
            || value == '|';
    }

    private static string CleanSlotIdText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        return new string(text.Where(IsAllowedSlotIdChar).ToArray());
    }

    private static string CleanDisplayNameText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        return new string(text.Where(c => !IsBlockedDisplayNameChar(c)).ToArray());
    }

    private void SlotIdTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(IsAllowedSlotIdChar);
    }

    private void DisplayNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = e.Text.Any(IsBlockedDisplayNameChar);
    }

    private void SlotIdTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        CleanIdentityPaste(sender, e, CleanSlotIdText);
    }

    private void DisplayNameTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        CleanIdentityPaste(sender, e, CleanDisplayNameText);
    }

    private static void CleanIdentityPaste(object sender, DataObjectPastingEventArgs e, Func<string?, string> cleaner)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var pastedText = e.DataObject.GetData(DataFormats.Text) as string ?? "";
        var cleanedText = cleaner(pastedText);
        if (string.IsNullOrEmpty(cleanedText))
        {
            e.CancelCommand();
            return;
        }

        if (string.Equals(pastedText, cleanedText, StringComparison.Ordinal))
        {
            return;
        }

        e.CancelCommand();

        if (sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        var selectionStart = textBox.SelectionStart;
        textBox.SelectedText = cleanedText;
        textBox.SelectionStart = selectionStart + cleanedText.Length;
        textBox.SelectionLength = 0;
    }

    private static string MakeSafeId(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "custom_batman_suit";
        }

        var safe = Regex.Replace(text.Trim().ToLowerInvariant(), "[^a-z0-9_]+", "_");
        safe = Regex.Replace(safe, "_+", "_").Trim('_');
        return string.IsNullOrWhiteSpace(safe) ? "custom_batman_suit" : safe;
    }

    private static string Shorten(string text, int max)
    {
        if (text.Length <= max)
        {
            return text;
        }

        return text[..Math.Max(0, max - 3)] + "...";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void MainPagesTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _discordPresence.SetPageActivity(MainPagesTabControl.SelectedIndex, DisplayName);
    }
}

public sealed class OperationRow : INotifyPropertyChanged
{
    private bool _enabled = true;
    private bool _isMarkedForDelete;
    private string _type = "set_texture_parameter";
    private string _component = "CharacterMesh0";
    private string _componentClass = "MeshComponent";
    private string _materialSlot = "0";
    private string _parameterName = "BC";
    private string _asset = "";
    private string _materialsCsv = "";
    private string _applyTo = "both";
    private bool _required;
    private string _parent = "";
    private string _socket = "";
    private bool _propagateToChildren;
    private bool _reinitializePose;
    private string _notes = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Enabled { get => _enabled; set => SetProperty(ref _enabled, value); }
    public bool IsMarkedForDelete { get => _isMarkedForDelete; set => SetProperty(ref _isMarkedForDelete, value); }
    public string Type { get => _type; set => SetProperty(ref _type, value); }
    public string Component { get => _component; set => SetProperty(ref _component, value); }
    public string ComponentClass { get => _componentClass; set => SetProperty(ref _componentClass, value); }
    public string MaterialSlot { get => _materialSlot; set => SetProperty(ref _materialSlot, value); }
    public string ParameterName { get => _parameterName; set => SetProperty(ref _parameterName, value); }
    public string Asset { get => _asset; set => SetProperty(ref _asset, value); }
    public string MaterialsCsv { get => _materialsCsv; set => SetProperty(ref _materialsCsv, value); }
    public string ApplyTo { get => _applyTo; set => SetProperty(ref _applyTo, value); }
    public bool Required { get => _required; set => SetProperty(ref _required, value); }
    public string Parent { get => _parent; set => SetProperty(ref _parent, value); }
    public string Socket { get => _socket; set => SetProperty(ref _socket, value); }
    public bool PropagateToChildren { get => _propagateToChildren; set => SetProperty(ref _propagateToChildren, value); }
    public bool ReinitializePose { get => _reinitializePose; set => SetProperty(ref _reinitializePose, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }

    public OperationRow Clone() => new()
    {
        Enabled = Enabled,
        Type = Type,
        Component = Component,
        ComponentClass = ComponentClass,
        MaterialSlot = MaterialSlot,
        ParameterName = ParameterName,
        Asset = Asset,
        MaterialsCsv = MaterialsCsv,
        ApplyTo = ApplyTo,
        Required = Required,
        Parent = Parent,
        Socket = Socket,
        PropagateToChildren = PropagateToChildren,
        ReinitializePose = ReinitializePose,
        Notes = Notes
    };

    public static OperationRow FromJson(JsonElement op, EditableFieldRegistry registry)
    {
        var type = GetString(op, "type") ?? "set_texture_parameter";
        var component = GetString(op, "component") ?? "CharacterMesh0";
        var componentClass = GetString(op, "component_class") ?? GetDefaultClass(component, registry);

        var row = new OperationRow
        {
            Enabled = GetBoolNullable(op, "enabled") ?? true,
            Type = type,
            Component = component,
            ComponentClass = componentClass,
            MaterialSlot = GetIntNullable(op, "material_slot")?.ToString() ?? "0",
            ParameterName = GetString(op, "parameter_name") ?? "BC",
            Asset = GetString(op, "asset") ?? "",
            MaterialsCsv = GetStringArrayCsv(op, "materials"),
            ApplyTo = GetString(op, "apply_to") ?? "both",
            Required = GetBoolNullable(op, "required") ?? false,
            Parent = GetString(op, "parent") ?? "",
            Socket = GetString(op, "socket") ?? "",
            PropagateToChildren = GetBoolNullable(op, "propagate_to_children") ?? false,
            ReinitializePose = GetBoolNullable(op, "reinitialize_pose") ?? false,
            Notes = ""
        };

        if (type == "set_visibility")
        {
            row.Asset = (GetBoolNullable(op, "visible") ?? true).ToString().ToLowerInvariant();
        }
        else if (type == "set_hidden_in_game")
        {
            row.Asset = (GetBoolNullable(op, "hidden") ?? true).ToString().ToLowerInvariant();
        }
        else if (type == "set_vector_parameter")
        {
            row.Asset = GetVectorValueCsv(op, "value")
                        ?? GetVectorValueCsv(op, "vector")
                        ?? GetString(op, "value")
                        ?? "1.0, 1.0, 1.0, 1.0";
        }
        else if (type == "set_scalar_parameter")
        {
            row.Asset = GetString(op, "value")
                        ?? GetString(op, "scalar_value")
                        ?? "0.0";
        }

        return row;
    }

    private static string GetDefaultClass(string component, EditableFieldRegistry registry)
    {
        return registry.ComponentClassDefaults.TryGetValue(component, out var cls) ? cls : "MeshComponent";
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static bool? GetBoolNullable(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var b) ? b : null,
            _ => null
        };
    }

    private static int? GetIntNullable(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i))
        {
            return i;
        }

        return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private static string? GetVectorValueCsv(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return string.Join(", ", value.EnumerateArray().Select(v => v.ValueKind == JsonValueKind.Number ? v.ToString() : v.GetString()).Where(v => !string.IsNullOrWhiteSpace(v)));
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            static string? ReadComponent(JsonElement obj, string lower, string upper)
            {
                if (obj.TryGetProperty(lower, out var lowerValue))
                {
                    return lowerValue.ToString();
                }

                return obj.TryGetProperty(upper, out var upperValue) ? upperValue.ToString() : null;
            }

            var r = ReadComponent(value, "r", "R");
            var g = ReadComponent(value, "g", "G");
            var b = ReadComponent(value, "b", "B");
            var a = ReadComponent(value, "a", "A") ?? "1.0";

            if (!string.IsNullOrWhiteSpace(r) && !string.IsNullOrWhiteSpace(g) && !string.IsNullOrWhiteSpace(b))
            {
                return $"{r}, {g}, {b}, {a}";
            }
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static string GetStringArrayCsv(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return "";
        }

        return string.Join(", ", value.EnumerateArray().Select(v => v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString()).Where(v => !string.IsNullOrWhiteSpace(v)));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed class EquipmentRow : INotifyPropertyChanged
{
    private bool _enabled = true;
    private string _slot = "0";
    private string _replaceEquipment = "";
    private string _withEquipment = "";
    private string _notes = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Enabled { get => _enabled; set => SetProperty(ref _enabled, value); }
    public string Slot { get => _slot; set => SetProperty(ref _slot, value); }
    public string ReplaceEquipment { get => _replaceEquipment; set => SetProperty(ref _replaceEquipment, value); }
    public string WithEquipment { get => _withEquipment; set => SetProperty(ref _withEquipment, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }

    public static EquipmentRow FromJson(JsonElement entry) => new()
    {
        Enabled = true,
        Slot = GetIntNullable(entry, "slot")?.ToString() ?? "0",
        ReplaceEquipment = GetString(entry, "replace_equipment") ?? "",
        WithEquipment = GetString(entry, "with_equipment") ?? "",
        Notes = ""
    };

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static int? GetIntNullable(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i))
        {
            return i;
        }

        return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed record PendingJsonFile(string Path, JsonDocument Document, bool IsSuitConfig);

public sealed class KnownEditableField : INotifyPropertyChanged
{
    private string _component = "CharacterMesh0";
    private string _componentClass = "MeshComponent";
    private string _parameterName = "BC";
    private string _primaryValue = "";
    private string _jsonPath = "";
    private string _detectedKinds = "";
    private string _summary = "";
    private string _materialDetails = "";
    private string _availableOperations = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Component { get => _component; set => SetProperty(ref _component, value); }
    public string ComponentClass { get => _componentClass; set => SetProperty(ref _componentClass, value); }
    public string ParameterName { get => _parameterName; set => SetProperty(ref _parameterName, value); }
    public string PrimaryValue { get => _primaryValue; set => SetProperty(ref _primaryValue, value); }
    public string JsonPath { get => _jsonPath; set => SetProperty(ref _jsonPath, value); }
    public string DetectedKinds { get => _detectedKinds; set => SetProperty(ref _detectedKinds, value); }
    public string Summary { get => _summary; set => SetProperty(ref _summary, value); }
    public string MaterialDetails { get => _materialDetails; set => SetProperty(ref _materialDetails, value); }
    public string AvailableOperations { get => _availableOperations; set => SetProperty(ref _availableOperations, value); }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed class DetectedField : INotifyPropertyChanged
{
    private bool _isSelected;
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public string Kind { get; set; } = "Known Field";
    public string Label { get; set; } = "";
    public string SuggestedOperationType { get; set; } = "set_texture_parameter";
    public string Component { get; set; } = "CharacterMesh0";
    public string ComponentClass { get; set; } = "MeshComponent";
    public string ParameterName { get; set; } = "BC";
    public string JsonPath { get; set; } = "";
    public string CurrentValue { get; set; } = "";

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed class EditableFieldRegistry
{
    public string Notes { get; set; } = "";
    public List<string> KnownComponents { get; set; } = [];
    public List<string> ComponentClasses { get; set; } = [];
    public Dictionary<string, string> ComponentClassDefaults { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> TextureParameters { get; set; } = [];
    public List<string> VectorParameters { get; set; } = [];
    public List<string> ScalarParameters { get; set; } = [];
    public List<string> OperationTypes { get; set; } = [];
    public List<string> ApplyToValues { get; set; } = [];
    public List<string> ScanKeywords { get; set; } = [];
    public List<PawnPreset> PawnPresets { get; set; } = [];

    public static EditableFieldRegistry Default() => new()
    {
        Notes = "Fallback defaults. Edit Data/EditableFieldRegistry.json for the real project list.",
        KnownComponents = ["CharacterMesh0", "Head", "Face", "Cape", "Torso", "Torso2", "NeckPeg", "Hip"],
        ComponentClasses = ["MeshComponent", "SkeletalMeshComponentBudgeted", "StaticMeshComponent", "SkeletalMeshComponent"],
        ComponentClassDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CharacterMesh0"] = "MeshComponent",
            ["Head"] = "SkeletalMeshComponentBudgeted",
            ["Face"] = "MeshComponent",
            ["Cape"] = "SkeletalMeshComponentBudgeted",
            ["Torso"] = "SkeletalMeshComponentBudgeted",
            ["Torso2"] = "SkeletalMeshComponentBudgeted",
            ["NeckPeg"] = "StaticMeshComponent",
            ["Hip"] = "StaticMeshComponent"
        },
        TextureParameters = ["BC", "BC_Pristine", "DNRM", "DNRM_Pristine", "MMR", "ColourMask"],
        VectorParameters = ["Base Color", "BaseColour", "Base Colour", "Grime_Colour", "Colour", "Color", "Emissive Colour", "Emissive_Colour", "EmissiveColour"],
        ScalarParameters = ["ColourChannel_Picker", "Rain Drizzle Scale", "Rain Droplet Scale", "Micro Detail Intensity", "ScratchTile", "Scratch_Height", "Scratch Mask Amount", "ScratchRoughness Intensity", "RefractionDepthBias"],
        OperationTypes = ["set_texture_parameter", "set_vector_parameter", "set_scalar_parameter", "set_material", "set_static_mesh", "set_skeletal_mesh", "clear_static_mesh", "clear_skeletal_mesh", "create_skeletal_component", "set_anim_class", "set_visibility", "set_hidden_in_game", "attach_component", "ensure_component"],
        ApplyToValues = ["both", "preview", "gameplay"],
        ScanKeywords = ["TextureParameterValues", "VectorParameterValues", "ScalarParameterValues", "Material", "Materials", "Equipment", "SkeletalMesh", "StaticMesh", "AnimClass", "Glider", "Wingsuit", "CharacterMesh0", "Head", "Face", "Cape", "Torso", "NeckPeg", "Hip"],
        PawnPresets =
        [
            new PawnPreset("TheBatman2025", "Pawns.Playable.Batman.TheBatman2025", "BP_Batman_TheBatman2025_Playable_C"),
            new PawnPreset("1943", "Pawns.Playable.Batman.1943", "BP_Batman_1943_Playable_C"),
            new PawnPreset("1966", "Pawns.Playable.Batman.1966", "BP_Batman_1966_Playable_C"),
            new PawnPreset("1989", "Pawns.Playable.Batman.1989", "BP_Batman_1989_Playable_C"),
            new PawnPreset("Absolute", "Pawns.Playable.Batman.Absolute", "BP_Batman_Absolute_Playable_C"),
            new PawnPreset("AndRobin", "Pawns.Playable.Batman.AndRobin", "BP_Batman_AndRobin_Playable_C"),
            new PawnPreset("AnimatedSeries", "Pawns.Playable.Batman.AnimatedSeries", "BP_Batman_AnimatedSeries_Playable_C"),
            new PawnPreset("ArkhamKnight", "Pawns.Playable.Batman.ArkhamKnight", "BP_Batman_ArkhamKnight_Playable_C"),
            new PawnPreset("ArmouredSuit", "Pawns.Playable.Batman.ArmouredSuit", "BP_Batman_ArmouredSuit_Playable_C"),
            new PawnPreset("Asylum", "Pawns.Playable.Batman.Asylum", "BP_Batman_Asylum_Playable_C"),
            new PawnPreset("Batman", "Pawns.Playable.Batman.Batman", "BP_Batman_Batman_Playable_C"),
            new PawnPreset("Begins", "Pawns.Playable.Batman.Begins", "BP_Batman_Begins_Playable_C"),
            new PawnPreset("Beyond", "Pawns.Playable.Batman.Beyond", "BP_Batman_Beyond_Playable_C"),
            new PawnPreset("BlackLantern", "Pawns.Playable.Batman.BlackLantern", "BP_Batman_BlackLantern_Playable_C"),
            new PawnPreset("BraveAndTheBold", "Pawns.Playable.Batman.BraveAndTheBold", "BP_Batman_BraveAndTheBold_Playable_C"),
            new PawnPreset("DarkKnight", "Pawns.Playable.Batman.DarkKnight", "BP_Batman_DarkKnight_Playable_C"),
            new PawnPreset("DarkKnightReturns", "Pawns.Playable.Batman.DarkKnightReturns", "BP_Batman_DarkKnightReturns_Playable_C"),
            new PawnPreset("DC27", "Pawns.Playable.Batman.DC27", "BP_Batman_DC27_Playable_C"),
            new PawnPreset("FinalBoss", "Pawns.Playable.Batman.FinalBoss", "BP_Batman_FinalBoss_Playable_C"),
            new PawnPreset("GothamByGaslight", "Pawns.Playable.Batman.GothamByGaslight", "BP_Batman_GothamByGaslight_Playable_C"),
            new PawnPreset("GrayGhost", "Pawns.Playable.Batman.GrayGhost", "BP_Batman_GrayGhost_Playable_C"),
            new PawnPreset("IceSuit", "Pawns.Playable.Batman.IceSuit", "BP_Batman_IceSuit_Playable_C"),
            new PawnPreset("JusticeLeague", "Pawns.Playable.Batman.JusticeLeague", "BP_Batman_JusticeLeague_Playable_C"),
            new PawnPreset("Knightfall", "Pawns.Playable.Batman.Knightfall", "BP_Batman_Knightfall_Playable_C"),
            new PawnPreset("Knightfight", "Pawns.Playable.Batman.Knightfight", "BP_Batman_Knightfight_Playable_C"),
            new PawnPreset("Knightmare", "Pawns.Playable.Batman.Knightmare", "BP_Batman_Knightmare_Playable_C"),
            new PawnPreset("LBM", "Pawns.Playable.Batman.LBM", "BP_Batman_LBM_Playable_C"),
            new PawnPreset("LEGOBatman08", "Pawns.Playable.Batman.LEGOBatman08", "BP_Batman_LEGOBatman08_Playable_C"),
            new PawnPreset("MaskOfTengu", "Pawns.Playable.Batman.MaskOfTengu", "BP_Batman_MaskOfTengu_Playable_C"),
            new PawnPreset("NES", "Pawns.Playable.Batman.NES", "BP_Batman_NES_Playable_C"),
            new PawnPreset("Ninja", "Pawns.Playable.Batman.Ninja", "BP_Batman_Ninja_Playable_C"),
            new PawnPreset("One2025", "Pawns.Playable.Batman.One2025", "BP_Batman_One2025_Playable_C"),
            new PawnPreset("Pirate", "Pawns.Playable.Batman.Pirate", "BP_Batman_Pirate_Playable_C"),
            new PawnPreset("Rainbow", "Pawns.Playable.Batman.Rainbow", "BP_Batman_Rainbow_Playable_C"),
            new PawnPreset("SonarSuit", "Pawns.Playable.Batman.SonarSuit", "BP_Batman_SonarSuit_Playable_C"),
            new PawnPreset("TtOriginal", "Pawns.Playable.Batman.TtOriginal", "BP_Batman_TtOriginal_Playable_C"),
            new PawnPreset("Vampire", "Pawns.Playable.Batman.Vampire", "BP_Batman_Vampire_Playable_C"),
            new PawnPreset("ZeroYear", "Pawns.Playable.Batman.ZeroYear", "BP_Batman_ZeroYear_Playable_C"),
            new PawnPreset("ZurEnArrh", "Pawns.Playable.Batman.ZurEnArrh", "BP_Batman_ZurEnArrh_Playable_C"),
            new PawnPreset("Keaton", "Pawns.Playable.Batman.Keaton", "BP_Batman_Keaton_Playable_C"),
            new PawnPreset("Frozen", "Pawns.Playable.Batman.Frozen", "BP_Batman_Frozen_Playable_C")
        ]
    };
}

public sealed record PawnPreset(string Name, string Tag, string ActorClassContains);

public sealed class ExportSuitConfig
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = 2;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("menu_order")]
    public int MenuOrder { get; set; } = 100;

    [JsonPropertyName("slot_id")]
    public string SlotId { get; set; } = "";

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("source_tag")]
    public string SourceTag { get; set; } = "Pawns.Playable.Batman.TheBatman2025";

    [JsonPropertyName("source_actor_class_contains")]
    public string SourceActorClassContains { get; set; } = "BP_Batman_TheBatman2025_Playable_C";

    [JsonPropertyName("icon_asset")]
    public string? IconAsset { get; set; }

    [JsonPropertyName("uimd_icons")]
    public UimdIconsConfig? UimdIcons { get; set; }

    [JsonPropertyName("operations")]
    public List<Dictionary<string, object?>> Operations { get; set; } = [];

    [JsonPropertyName("equipment_replacements")]
    public List<ExportEquipmentReplacement> EquipmentReplacements { get; set; } = [];
}

public sealed class UimdIconsConfig
{
    [JsonPropertyName("menu_icon")]
    public string? MenuIcon { get; set; }

    [JsonPropertyName("right_facing")]
    public string? RightFacing { get; set; }

    [JsonPropertyName("left_facing")]
    public string? LeftFacing { get; set; }
}

public sealed class ExportEquipmentReplacement
{
    [JsonPropertyName("slot")]
    public int Slot { get; set; }

    [JsonPropertyName("replace_equipment")]
    public string ReplaceEquipment { get; set; } = "";

    [JsonPropertyName("with_equipment")]
    public string WithEquipment { get; set; } = "";

}
