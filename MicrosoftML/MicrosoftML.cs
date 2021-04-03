using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.DataManagment_Engine.AI;
using Microsoft.ML.Data;
using Microsoft.ML;
using static Microsoft.ML.DataOperationsCatalog;
using System.IO;
using TheTechIdea.DataManagment_Engine;
using TheTechIdea.Util;
using TheTechIdea;

namespace MicrosoftML
{
    public class MicrosoftML : IAAPP
    {
        public IDMEEditor DMEEditor { get; set; }

        public readonly ITransformer _trainedModel;
        public readonly string modelPath;
        public readonly MLContext mlContext;
        public ITransformer model;
        public TrainTestData splitDataView;
        public IDataView dataView;
        public IDataView splitTestSet;
        public IDataView predictions;
       
        static readonly string _dataPath = Path.Combine(Environment.CurrentDirectory, "Data", "yelp_labelled.txt");
#pragma warning disable CS0169 // The field 'MicrosoftML.predictionFunction' is never used
        static PredictionEngine<object, object> predictionFunction;
#pragma warning restore CS0169 // The field 'MicrosoftML.predictionFunction' is never used


        public MicrosoftML()
        {
            AppID = Guid.NewGuid().ToString();

            mlContext = new MLContext();

            // Load model from file.
         
           
        }
        public string AppName { get ; set ; }

        public string AppID { get; }

        public string DataSourceName { get ; set ; }
        public List<string> Tables { get ; set ; }
        public List<DataTable> TestData { get ; set ; }

        #region "MS ML Methods"
        public  ITransformer BuildAndTrainModel(MLContext mlContext, IDataView splitTrainSet,string _outputColumnname,string _inputColumnName,string ColumnPredicate)
        {
            var estimator = mlContext.Transforms.Text.FeaturizeText(outputColumnName: _outputColumnname, inputColumnName: _inputColumnName).Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: ColumnPredicate, featureColumnName: "Features"));
            Console.WriteLine("=============== Create and Train the Model ===============");
            var model = estimator.Fit(splitTrainSet);
            Console.WriteLine("=============== End of training ===============");
            Console.WriteLine();
            return model;
        }
        public ITransformer LoadModel(string _modelPath)
        {
            return  mlContext.Model.Load(_modelPath, out var modelInputSchema);
        }
        private static void UseModelWithSingleItem(MLContext mlContext, ITransformer model)
        {
            PredictionEngine<object,object> predictionFunction = mlContext.Model.CreatePredictionEngine<object, object>(model);
        }
        public static void UseModelWithBatchItems(MLContext mlContext, ITransformer model)
        {

        }
        #endregion
        #region "Exposed ML Methods"
        [MLPredict(Caption = "Predict")]
        public IEnumerable<T> Predict<T>(PassedArgs arg) where T: class,new()
        {
            IEnumerable<T> predictedResults;
            List<object> ls = new List<object>();
            try
            {
                if (arg.Objects != null)
                {
                    if (arg.Objects.Any(c => c.Name == "PREDICTDATA"))
                    {
                        ls =(List<object>) arg.Objects.Where(c => c.Name == "PREDICTDATA").FirstOrDefault().obj;
                    }
                    IDataView batchComments = mlContext.Data.LoadFromEnumerable(ls);

                    IDataView predictions = model.Transform(batchComments);

                    // Use model to predict whether comment data is Positive (1) or Negative (0).
                    predictedResults = (IEnumerable<T>)mlContext.Data.CreateEnumerable<T>(predictions, reuseRowObject: false);
                   
                }
                else
                {
                    return null;
                }

              

            }
            catch (Exception ex)
            {
                string mes = "Could not Run Predict";
               
                DMEEditor.AddLogMessage("Fail", ex.Message + " " + mes, DateTime.Now, -1, mes, Errors.Failed);
                return null; ;
            };
            return predictedResults;
        }
        #region "LoadData"
        [MLMethod(Caption = "Load Data Text File")]
        public IDataView LoadTextFile<T>(PassedArgs arg)
        {
            string datapath = null;
            IDataView dv=null;
            try
            {
                if (arg.Objects != null)
                {
                    if (arg.Objects.Any(c => c.Name == "DATAPATH"))
                    {
                        datapath = arg.Objects.Where(c => c.Name == "DATAPATH").FirstOrDefault().obj.ToString();
                    }
                   
                }
                if ( datapath != null)
                {
                    dv = mlContext.Data.LoadFromTextFile<T>(datapath, hasHeader: false);
                    DMEEditor.AddLogMessage("Success", $"Loaded Text File {datapath}", DateTime.Now, -1, null, Errors.Ok);
                }
                else
                {
                    string mes = $"Could not find datapath";
                    DMEEditor.AddLogMessage("Fail", mes, DateTime.Now, -1, mes, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                string mes = $"Could not Loaded Text File { datapath}";
                DMEEditor.AddLogMessage("Fail", ex.Message + " " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return dv;
        }
        [MLMethod(Caption = "Load Binary File")]
        public IDataView LoadFromBinary(PassedArgs arg)
        {
            string datapath = null;
            IDataView dv = null;
            
            try
            {
                if (arg.Objects != null)
                {
                    if (arg.Objects.Any(c => c.Name == "DATAPATH"))
                    {
                        datapath = arg.Objects.Where(c => c.Name == "DATAPATH").FirstOrDefault().obj.ToString();
                    }

                }
                if (datapath != null)
                {
                   
                 
                    dv = mlContext.Data.LoadFromBinary(datapath);
                    DMEEditor.AddLogMessage("Success", $"Loaded Text File {datapath}", DateTime.Now, -1, null, Errors.Ok);
                }
                else
                {
                    string mes = $"Could not find datapath";
                    DMEEditor.AddLogMessage("Fail", mes, DateTime.Now, -1, mes, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                string mes = $"Could not Loaded Text File { datapath}";
                DMEEditor.AddLogMessage("Fail", ex.Message + " " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return dv;
        }
        [MLMethod(Caption = "Load SvmLight File")]
        public IDataView LoadFromSvmLightFile(PassedArgs arg)
        {
            string datapath = null;
            IDataView dv = null;
            try
            {
                if (arg.Objects != null)
                {
                    if (arg.Objects.Any(c => c.Name == "DATAPATH"))
                    {
                        datapath = arg.Objects.Where(c => c.Name == "DATAPATH").FirstOrDefault().obj.ToString();
                    }

                }
                if (datapath != null)
                {
                    dv = mlContext.Data.LoadFromSvmLightFile(datapath);
                    DMEEditor.AddLogMessage("Success", $"Loaded Text File {datapath}", DateTime.Now, -1, null, Errors.Ok);
                }
                else
                {
                    string mes = $"Could not find datapath";
                    DMEEditor.AddLogMessage("Fail", mes, DateTime.Now, -1, mes, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                string mes = $"Could not Loaded Text File { datapath}";
                DMEEditor.AddLogMessage("Fail", ex.Message + " " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return dv;
        }
        [MLMethod(Caption = "Load SvmLight with Feature Names File")]
        public IDataView LoadFromSvmLightFileWithFeatureNames(PassedArgs arg)
        {
            string datapath = null;
            IDataView dv = null;
            try
            {
                if (arg.Objects != null)
                {
                    if (arg.Objects.Any(c => c.Name == "DATAPATH"))
                    {
                        datapath = arg.Objects.Where(c => c.Name == "DATAPATH").FirstOrDefault().obj.ToString();
                    }

                }
                if (datapath != null)
                {
                    dv = mlContext.Data.LoadFromSvmLightFileWithFeatureNames(datapath);
                    DMEEditor.AddLogMessage("Success", $"Loaded Text File {datapath}", DateTime.Now, -1, null, Errors.Ok);
                }
                else
                {
                    string mes = $"Could not find datapath";
                    DMEEditor.AddLogMessage("Fail", mes, DateTime.Now, -1, mes, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                string mes = $"Could not Loaded Text File { datapath}";
                DMEEditor.AddLogMessage("Fail", ex.Message + " " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return dv;
        }
        [MLMethod(Caption = "Load SvmLight with Feature Names File")]
        public IDataView LoadFromEnumerable<T>(PassedArgs arg) where T : class, new()
        {
            IEnumerable<T> data = null;
            IDataView dv = null;
            DataViewSchema sc=null;
            try
            {
                if (arg.Objects != null)
                {
                    if (arg.Objects.Any(c => c.Name == "DATA"))
                    {
                        data = (IEnumerable<T>) arg.Objects.Where(c => c.Name == "DATA").FirstOrDefault().obj;
                    }
                    if (arg.Objects.Any(c => c.Name == "DATAVIEWSCHEMA"))
                    {
                        sc =(DataViewSchema) arg.Objects.Where(c => c.Name == "DATAVIEWSCHEMA").FirstOrDefault().obj;
                    }

                }
                if (data != null)
                {
                  
                    dv = mlContext.Data.LoadFromEnumerable<T>(data,sc);
                    DMEEditor.AddLogMessage("Success", $"Loaded Text File ", DateTime.Now, -1, null, Errors.Ok);
                }
                else
                {
                    string mes = $"Could not find datapath";
                    DMEEditor.AddLogMessage("Fail", mes, DateTime.Now, -1, mes, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                string mes = $"Could not Loaded Text File ";
                DMEEditor.AddLogMessage("Fail", ex.Message + " " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return dv;
        }
        [MLMethod(Caption = "Load SvmLight with Feature Names File")]
        public TrainTestData TrainSplitData(PassedArgs arg) 
        {
           
            IDataView dv =null;
           
            double testfraction = .1;
            try
            {
                if (arg.Objects != null)
                {
                    
                    if (arg.Objects.Any(c => c.Name == "IDATAVIEW"))
                    {
                        dv = (IDataView)arg.Objects.Where(c => c.Name == "IDATAVIEW").FirstOrDefault().obj;
                    }
                    if (arg.Objects.Any(c => c.Name == "TESTFRACTION"))
                    {
                        testfraction = Convert.ToDouble(arg.Objects.Where(c => c.Name == "TESTFRACTION").FirstOrDefault().obj);
                    }

                }
                if (dv != null )
                {

                    splitDataView = mlContext.Data.TrainTestSplit(dv, testFraction: testfraction);
                    DMEEditor.AddLogMessage("Success", $"Splitted Data", DateTime.Now, -1, null, Errors.Ok);
                }
                else
                {
                    string mes = $"Could not Split Data";
                    DMEEditor.AddLogMessage("Fail", mes, DateTime.Now, -1, mes, Errors.Failed);
                }
            }
            catch (Exception ex)
            {
                string mes = $"Could not Splitted Data";
                DMEEditor.AddLogMessage("Fail", ex.Message + " " + mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return splitDataView;
        }
        #endregion
        #region "TrainandLearn"
        [MLMethod(Caption = "binary classification")]
        public IErrorsInfo binaryclassification<T>(PassedArgs arg) 
        {
            string datapath = null;
            string outputColumnname = null;
            string inputColumnName = null;
            string columnPredicate = null;
            double testfraction =.1;
            object type;
          //  bool ok=false;
            try
            {
                if (arg.Objects!=null)
                {
                    if (arg.Objects.Any(c => c.Name == "DATAPATH"))
                    {
                        datapath = arg.Objects.Where(c => c.Name == "DATAPATH").FirstOrDefault().obj.ToString();
                    }
                    if (arg.Objects.Any(c => c.Name == "OUTPUTCOLUMNNAME"))
                    {
                        outputColumnname = arg.Objects.Where(c => c.Name == "OUTPUTCOLUMNNAME").FirstOrDefault().obj.ToString();
                    }
                    if (arg.Objects.Any(c => c.Name == "INPUTCOLUMNNAME"))
                    {
                        inputColumnName = arg.Objects.Where(c => c.Name == "INPUTCOLUMNNAME").FirstOrDefault().obj.ToString();
                    }
                    if (arg.Objects.Any(c => c.Name == "COLUMNPREDICATE"))
                    {
                        columnPredicate = arg.Objects.Where(c => c.Name == "COLUMNPREDICATE").FirstOrDefault().obj.ToString();
                    }
                    if (arg.Objects.Any(c => c.Name == "DATACLASS"))
                    {
                        type = arg.Objects.Where(c => c.Name == "DATACLASS").FirstOrDefault().obj; 
                    }
                    if (arg.Objects.Any(c => c.Name == "TESTFRACTION"))
                    {
                        testfraction = Convert.ToDouble(arg.Objects.Where(c => c.Name == "TESTFRACTION").FirstOrDefault().obj);
                    }


                }
                if(columnPredicate!=null && inputColumnName!=null && outputColumnname!=null && datapath != null)
                {
                    
                    dataView = mlContext.Data.LoadFromTextFile<T>(_dataPath, hasHeader: false);
                    splitDataView = mlContext.Data.TrainTestSplit(dataView, testFraction: testfraction);
                    var estimator = mlContext.Transforms.Text.FeaturizeText(outputColumnName: outputColumnname, inputColumnName: inputColumnName).Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: columnPredicate, featureColumnName: "Features"));
                    splitTestSet = splitDataView.TestSet;


                    Console.WriteLine("=============== Create and Train the Model ===============");
                    model = estimator.Fit(splitTestSet);
                    Console.WriteLine("=============== End of training ===============");
                    Console.WriteLine();

                    Console.WriteLine("=============== Evaluating Model accuracy with Test data===============");
                    predictions = model.Transform(splitTestSet);

                    CalibratedBinaryClassificationMetrics metrics = mlContext.BinaryClassification.Evaluate(predictions, columnPredicate);

                    Console.WriteLine();
                    Console.WriteLine("Model quality metrics evaluation");
                    Console.WriteLine("--------------------------------");
                    Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
                    Console.WriteLine($"Auc: {metrics.AreaUnderRocCurve:P2}");
                    Console.WriteLine($"F1Score: {metrics.F1Score:P2}");
                    Console.WriteLine("=============== End of model evaluation ===============");
                    DMEEditor.AddLogMessage("Success", "Ran binary classification" , DateTime.Now, -1, null, Errors.Ok);
                }
                else
                {
                    string mes = "Could not Locate all Needed Parameters";
                    DMEEditor.AddLogMessage("Fail", mes, DateTime.Now, -1, mes, Errors.Failed);
                }
                
               
            }
            catch (Exception ex)
            {
                string mes = "Could not Locate all Needed Parameters";
                DMEEditor.AddLogMessage("Fail",ex.Message+" "+mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [MLMethod(Caption = "multiclass classification")]
        public IErrorsInfo multiclassclassification()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [MLMethod(Caption = "regression")]
        public IErrorsInfo regression()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [MLMethod(Caption = "k-means clustering")]
        public IErrorsInfo kmeansclustering()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [MLMethod(Caption = "matrix factorization")]
        public IErrorsInfo matrixfactorization()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [MLMethod(Caption = "Image Classification")]
        public IErrorsInfo ImageClassification()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [MLMethod(Caption = "Image classification model from a pre-trained TensorFlow model")]
        public IErrorsInfo imageclassificationmodelfromapretrainedTensorFlowmodel()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [MLMethod(Caption = "time series analysis")]
        public IErrorsInfo timeseriesanalysis()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [MLMethod(Caption = "anomaly detection for time series data")]
        public IErrorsInfo anomalydetectionfortimeseriesdata()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [MLMethod(Caption = "anomaly detection")]
        public IErrorsInfo anomalydetection()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [MLMethod(Caption = "detect objects in images using a pre-trained ONNX model")]
        public IErrorsInfo detectobjectsinimagesusingapretrainedONNXmodel()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [MLMethod(Caption = "binary classification using a pre-trained TensorFlow model")]
        public IErrorsInfo binaryclassificationusingapretrainedTensorFlowmodel()
        {

            try
            {

            }
            catch (Exception ex)
            {
                string mes = "Could not Add Category";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion

        #endregion
    }
}
