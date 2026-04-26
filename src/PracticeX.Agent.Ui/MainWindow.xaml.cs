using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using Microsoft.Win32;
using PracticeX.Agent.Cli.Http;
using PracticeX.Agent.Cli.Inventory;

namespace PracticeX.Agent.Ui;

public partial class MainWindow : Window
{
    public ObservableCollection<ScoredRowVm> Rows { get; } = new();
    private Guid? _manifestBatchId;
    private string? _scanRoot;
    private List<ManifestItemDto> _manifestItems = new();

    public MainWindow()
    {
        InitializeComponent();
        ResultsGrid.ItemsSource = Rows;
        Loaded += async (_, _) => await RefreshConnectionsAsync();
    }

    private bool Insecure => true; // dev default; the API uses self-signed certs locally
    private string? Token => Environment.GetEnvironmentVariable("PRACTICEX_TOKEN");

    private async Task RefreshConnectionsAsync()
    {
        if (!Uri.TryCreate(ApiBox.Text, UriKind.Absolute, out var apiUri))
        {
            SetStatus("Invalid API URL.", isError: true);
            return;
        }

        SetStatus("Loading connections...");
        try
        {
            var connections = await PracticeXClient.ListConnectionsAsync(apiUri, Token, Insecure, default);
            var folders = connections.Where(c => c.SourceType == "local_folder").ToList();
            ConnectionCombo.ItemsSource = folders.Select(c => new ConnectionOption(c)).ToList();
            if (folders.Count == 0)
            {
                SetStatus("No local_folder connections found. Create one in the web UI first.", isError: true);
            }
            else
            {
                ConnectionCombo.SelectedIndex = 0;
                SetStatus($"Loaded {folders.Count} local_folder connection(s).");
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to load connections: {ex.Message}", isError: true);
        }
    }

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Pick a folder to scan",
            InitialDirectory = Directory.Exists(FolderBox.Text) ? FolderBox.Text : @"C:\"
        };
        if (dlg.ShowDialog(this) == true)
        {
            FolderBox.Text = dlg.FolderName;
        }
    }

    private async void OnRefreshConnections(object sender, RoutedEventArgs e) => await RefreshConnectionsAsync();

    private async void OnScan(object sender, RoutedEventArgs e)
    {
        if (ConnectionCombo.SelectedItem is not ConnectionOption conn)
        {
            SetStatus("Pick a connection first.", isError: true);
            return;
        }
        if (!Uri.TryCreate(ApiBox.Text, UriKind.Absolute, out var apiUri))
        {
            SetStatus("Invalid API URL.", isError: true);
            return;
        }
        if (!Directory.Exists(FolderBox.Text))
        {
            SetStatus($"Folder does not exist: {FolderBox.Text}", isError: true);
            return;
        }

        ScanBtn.IsEnabled = false;
        UploadBtn.IsEnabled = false;
        Rows.Clear();
        ResetCounts();
        SetStatus($"Inventorying {FolderBox.Text}...");

        try
        {
            _scanRoot = FolderBox.Text;
            _manifestItems = await Task.Run(() => FolderEnumerator.Enumerate(_scanRoot).ToList());
            if (_manifestItems.Count == 0)
            {
                SetStatus("No files passed inventory filters.", isError: true);
                return;
            }

            SetStatus($"Inventoried {_manifestItems.Count} files. Posting metadata-only manifest...");

            using var client = new PracticeXClient(apiUri, conn.Id, Token, Insecure);
            var response = await client.PostManifestAsync(_manifestItems, notes: null, default);

            _manifestBatchId = response.BatchId;
            TotalText.Text = response.TotalItems.ToString();
            StrongText.Text = response.StrongCount.ToString();
            LikelyText.Text = response.LikelyCount.ToString();
            PossibleText.Text = response.PossibleCount.ToString();
            SkippedText.Text = response.SkippedCount.ToString();

            foreach (var item in response.Items)
            {
                var row = new ScoredRowVm(item);
                row.PropertyChanged += (_, _) => UpdateSelectionLabel();
                Rows.Add(row);
            }

            ApplyDefaultSelection();
            SetStatus($"Scan complete. Manifest batch {response.BatchId} (phase=manifest). Pick rows to upload, then click Upload selected.");
        }
        catch (Exception ex)
        {
            SetStatus($"Scan failed: {ex.Message}", isError: true);
        }
        finally
        {
            ScanBtn.IsEnabled = true;
        }
    }

    private async void OnUpload(object sender, RoutedEventArgs e)
    {
        if (_manifestBatchId is null || _scanRoot is null)
        {
            SetStatus("Run a scan first.", isError: true);
            return;
        }
        if (ConnectionCombo.SelectedItem is not ConnectionOption conn)
        {
            SetStatus("Pick a connection first.", isError: true);
            return;
        }
        if (!Uri.TryCreate(ApiBox.Text, UriKind.Absolute, out var apiUri))
        {
            return;
        }

        var selected = Rows.Where(r => r.IsSelected && r.Band != ManifestBandNames.Skipped).ToList();
        if (selected.Count == 0)
        {
            SetStatus("Nothing selected.", isError: true);
            return;
        }

        var bundleFiles = MapBundle(selected);
        UploadBtn.IsEnabled = false;
        ScanBtn.IsEnabled = false;
        SetStatus($"Uploading {selected.Count} file(s) as bundle to batch {_manifestBatchId}...");

        try
        {
            using var client = new PracticeXClient(apiUri, conn.Id, Token, Insecure);
            var summary = await client.PostBundleAsync(_manifestBatchId.Value, bundleFiles, notes: null, default);

            SetStatus($"Bundle complete. Status={summary.Status} | candidates={summary.CandidateCount} duplicates={summary.SkippedCount} errors={summary.ErrorCount}.");
            // Once uploaded, the manifest batch is complete and can't accept more files.
            _manifestBatchId = null;
        }
        catch (Exception ex)
        {
            SetStatus($"Upload failed: {ex.Message}", isError: true);
        }
        finally
        {
            ScanBtn.IsEnabled = true;
            UpdateSelectionLabel();
        }
    }

    private List<BundleFile> MapBundle(IEnumerable<ScoredRowVm> rows)
    {
        var mimeByPath = _manifestItems
            .GroupBy(i => i.RelativePath)
            .ToDictionary(g => g.Key, g => g.First().MimeType ?? "application/octet-stream");

        return rows.Select(r => new BundleFile(
            AbsolutePath: Path.GetFullPath(Path.Combine(_scanRoot!, r.RelativePath.Replace('/', Path.DirectorySeparatorChar))),
            RelativePath: r.RelativePath,
            Name: r.Name,
            MimeType: mimeByPath.GetValueOrDefault(r.RelativePath, "application/octet-stream"),
            ManifestItemId: r.ManifestItemId)).ToList();
    }

    private void OnSelectStrongLikely(object sender, RoutedEventArgs e) => SelectByPredicate(r => r.Band is ManifestBandNames.Strong or ManifestBandNames.Likely);
    private void OnSelectAll(object sender, RoutedEventArgs e) => SelectByPredicate(r => r.Band != ManifestBandNames.Skipped);
    private void OnClearSelection(object sender, RoutedEventArgs e) => SelectByPredicate(_ => false);

    private void SelectByPredicate(Func<ScoredRowVm, bool> shouldSelect)
    {
        foreach (var row in Rows)
        {
            row.IsSelected = shouldSelect(row);
        }
        UpdateSelectionLabel();
    }

    private void ApplyDefaultSelection() => SelectByPredicate(r => r.Band is ManifestBandNames.Strong or ManifestBandNames.Likely);

    private void UpdateSelectionLabel()
    {
        var count = Rows.Count(r => r.IsSelected && r.Band != ManifestBandNames.Skipped);
        SelectionText.Text = $"{count} selected";
        UploadBtn.IsEnabled = count > 0 && _manifestBatchId is not null;
    }

    private void ResetCounts()
    {
        TotalText.Text = "0";
        StrongText.Text = "0";
        LikelyText.Text = "0";
        PossibleText.Text = "0";
        SkippedText.Text = "0";
        SelectionText.Text = "0 selected";
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? (System.Windows.Media.Brush)FindResource("BadBrush")
            : (System.Windows.Media.Brush)FindResource("MutedBrush");
    }
}

public sealed class ConnectionOption
{
    public ConnectionOption(SourceConnectionDto dto)
    {
        Id = dto.Id;
        Display = $"{dto.DisplayName ?? "(unnamed)"} — {dto.Status} — {dto.Id}";
    }

    public Guid Id { get; }
    public string Display { get; }
}

public sealed class ScoredRowVm : INotifyPropertyChanged
{
    private bool _isSelected;

    public ScoredRowVm(ManifestScoredItemDto item)
    {
        ManifestItemId = item.ManifestItemId;
        RelativePath = item.RelativePath;
        Name = item.Name;
        Band = item.Band;
        CandidateType = item.CandidateType;
        Confidence = item.Confidence;
        ReasonsDisplay = string.Join(" · ", item.ReasonCodes);
    }

    public string ManifestItemId { get; }
    public string RelativePath { get; }
    public string Name { get; }
    public string Band { get; }
    public string CandidateType { get; }
    public decimal Confidence { get; }
    public string ConfidenceDisplay => Confidence.ToString("0.00");
    public string ReasonsDisplay { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
