using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NPhies_FHIR_Integration.Application.Services.MachineLearning;

/// <summary>
/// Machine Learning Pipeline Service Interface
/// Provides ML data preparation, feature engineering, model management, and predictions
/// </summary>
public interface IMLPipelineService
{
    // ========== DATA PREPARATION ==========
    /// <summary>
    /// Prepare data for ML model training
    /// </summary>
    Task<MLDataset> PrepareDatasetAsync(
        DateTime startDate,
      DateTime endDate,
        string? dataType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean and validate data
    /// </summary>
    Task<DataQualityReport> ValidateDataAsync(
    MLDataset dataset,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Split data into training and testing sets
    /// </summary>
    Task<TrainTestSplit> SplitDatasetAsync(
        MLDataset dataset,
        double trainPercentage = 0.8,
     CancellationToken cancellationToken = default);

    // ========== FEATURE ENGINEERING ==========
    /// <summary>
    /// Extract features from raw data
    /// </summary>
    Task<FeatureSet> ExtractFeaturesAsync(
 MLDataset dataset,
        List<string>? selectedFeatures = null,
        CancellationToken cancellationToken = default);

    /// <summary>
  /// Normalize features
  /// </summary>
    Task<FeatureSet> NormalizeFeaturesAsync(
        FeatureSet features,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handle missing values
    /// </summary>
    Task<FeatureSet> HandleMissingValuesAsync(
     FeatureSet features,
        string strategy = "mean",
   CancellationToken cancellationToken = default);

    /// <summary>
    /// Encode categorical features
    /// </summary>
    Task<FeatureSet> EncodeCategoricalFeaturesAsync(
  FeatureSet features,
      CancellationToken cancellationToken = default);

    // ========== MODEL MANAGEMENT ==========
    /// <summary>
    /// Train ML model
  /// </summary>
    Task<MLModel> TrainModelAsync(
        string modelName,
        FeatureSet trainingFeatures,
     List<float> trainingLabels,
     MLModelConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluate model performance
    /// </summary>
    Task<ModelPerformance> EvaluateModelAsync(
        MLModel model,
        FeatureSet testFeatures,
        List<float> testLabels,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or create model
    /// </summary>
    Task<MLModel> GetOrCreateModelAsync(
        string modelName,
        MLModelType modelType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save model
    /// </summary>
    Task<bool> SaveModelAsync(
  MLModel model,
        string filePath,
  CancellationToken cancellationToken = default);

    /// <summary>
    /// Load model
    /// </summary>
    Task<MLModel?> LoadModelAsync(
 string modelName,
        string filePath,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Version model
    /// </summary>
    Task<string> VersionModelAsync(
        MLModel model,
        string version,
      string description,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Get model versions
    /// </summary>
    Task<List<ModelVersion>> GetModelVersionsAsync(
        string modelName,
CancellationToken cancellationToken = default);

    // ========== PREDICTIONS ==========
    /// <summary>
    /// Make single prediction
    /// </summary>
    Task<MLPrediction> PredictAsync(
   MLModel model,
        Dictionary<string, float> features,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Make batch predictions
    /// </summary>
    Task<List<MLPrediction>> PredictBatchAsync(
        MLModel model,
      List<Dictionary<string, float>> featuresList,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrain model with new data
    /// </summary>
    Task<MLModel> RetrainModelAsync(
        MLModel currentModel,
        FeatureSet newFeatures,
        List<float> newLabels,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get feature importance
    /// </summary>
    Task<Dictionary<string, double>> GetFeatureImportanceAsync(
    MLModel model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitor model performance
    /// </summary>
    Task<ModelHealthReport> MonitorModelHealthAsync(
        string modelName,
      CancellationToken cancellationToken = default);
}

// ========== DATA MODELS ==========

/// <summary>
/// ML Dataset
/// </summary>
public class MLDataset
{
    public string DatasetId { get; set; } = Guid.NewGuid().ToString();
    public string DatasetName { get; set; } = string.Empty;
    public int RecordCount { get; set; }
 public int FeatureCount { get; set; }
    public List<string> FeatureNames { get; set; } = new();
    public List<Dictionary<string, object?>> Data { get; set; } = new();
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string DataType { get; set; } = string.Empty; // Claims, Appeals, etc.
}

/// <summary>
/// Data quality report
/// </summary>
public class DataQualityReport
{
    public string DatasetId { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public int RecordsWithMissingValues { get; set; }
    public double QualityScore { get; set; } // 0-100
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Train/test split
/// </summary>
public class TrainTestSplit
{
  public MLDataset TrainingDataset { get; set; } = new();
 public MLDataset TestingDataset { get; set; } = new();
    public double TrainingPercentage { get; set; }
    public double TestingPercentage { get; set; }
}

/// <summary>
/// Feature set
/// </summary>
public class FeatureSet
{
    public string FeatureSetId { get; set; } = Guid.NewGuid().ToString();
    public List<string> FeatureNames { get; set; } = new();
    public List<List<float>> FeatureValues { get; set; } = new();
    public int RecordCount { get; set; }
    public int FeatureCount { get; set; }
    public Dictionary<string, FeatureStatistics> FeatureStats { get; set; } = new();
}

/// <summary>
/// Feature statistics
/// </summary>
public class FeatureStatistics
{
    public string FeatureName { get; set; } = string.Empty;
    public double Mean { get; set; }
    public double StdDev { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public int MissingCount { get; set; }
}

/// <summary>
/// ML Model
/// </summary>
public class MLModel
{
    public string ModelId { get; set; } = Guid.NewGuid().ToString();
    public string ModelName { get; set; } = string.Empty;
    public MLModelType ModelType { get; set; }
    public string Version { get; set; } = "1.0.0";
    public DateTime TrainedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedDate { get; set; }
    public ModelPerformance? Performance { get; set; }
    public List<string> FeatureNames { get; set; } = new();
    public Dictionary<string, object?> Metadata { get; set; } = new();
    public byte[]? ModelData { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// ML Model Type
/// </summary>
public enum MLModelType
{
    LinearRegression,
    LogisticRegression,
    RandomForest,
    GradientBoosting,
    NeuralNetwork,
    SVM,
  KNN
}

/// <summary>
/// ML Model Config
/// </summary>
public class MLModelConfig
{
    public MLModelType ModelType { get; set; } = MLModelType.RandomForest;
    public double LearningRate { get; set; } = 0.01;
    public int Epochs { get; set; } = 100;
    public int BatchSize { get; set; } = 32;
    public double ValidationSplit { get; set; } = 0.2;
    public Dictionary<string, object?>? Hyperparameters { get; set; }
}

/// <summary>
/// Model performance
/// </summary>
public class ModelPerformance
{
    public string ModelId { get; set; } = string.Empty;
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double AUC { get; set; }
    public double MAE { get; set; } // Mean Absolute Error
 public double RMSE { get; set; } // Root Mean Squared Error
  public int TruePositives { get; set; }
    public int TrueNegatives { get; set; }
 public int FalsePositives { get; set; }
    public int FalseNegatives { get; set; }
    public DateTime EvaluatedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// ML Prediction
/// </summary>
public class MLPrediction
{
    public string PredictionId { get; set; } = Guid.NewGuid().ToString();
    public string ModelId { get; set; } = string.Empty;
    public string InputRecordId { get; set; } = string.Empty;
    public float PredictedValue { get; set; }
    public double Confidence { get; set; } // 0-1
    public Dictionary<string, float>? FeatureContribution { get; set; }
    public string PredictionClass { get; set; } = string.Empty;
public DateTime PredictionDate { get; set; } = DateTime.UtcNow;
    public List<float> PredictionProbabilities { get; set; } = new();
}

/// <summary>
/// Model version
/// </summary>
public class ModelVersion
{
    public string ModelId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public ModelPerformance? Performance { get; set; }
    public bool IsCurrent { get; set; }
}

/// <summary>
/// Model health report
/// </summary>
public class ModelHealthReport
{
 public string ModelName { get; set; } = string.Empty;
 public string CurrentVersion { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = string.Empty; // Healthy, Degrading, Poor
    public double PerformanceScore { get; set; } // 0-100
    public int PredictionsMadeToday { get; set; }
public double AverageConfidence { get; set; }
    public DateTime LastRetrainingDate { get; set; }
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object?>? Metrics { get; set; }
}
