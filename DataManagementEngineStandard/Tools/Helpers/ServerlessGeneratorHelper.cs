using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Tools;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.Tools.Helpers
{
    /// <summary>
    /// Helper class for generating serverless and cloud-native components from entity structures.
    /// </summary>
    public class ServerlessGeneratorHelper
    {
        private readonly IDMEEditor _dmeEditor;
        private readonly ClassGenerationHelper _helper;

        /// <summary>
        /// Initializes a new instance of the ServerlessGeneratorHelper class.
        /// </summary>
        /// <param name="dmeEditor">The DMEEditor instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when dmeEditor is null.</exception>
        public ServerlessGeneratorHelper(IDMEEditor dmeEditor)
        {
            if (dmeEditor == null)
            {
                throw new ArgumentNullException(nameof(dmeEditor), "DMEEditor cannot be null.");
            }

            _dmeEditor = dmeEditor;
            _helper = new ClassGenerationHelper(dmeEditor);
        }

        /// <summary>
        /// Generates gRPC service definitions for an entity.
        /// </summary>
        /// <param name="entity">The entity structure to generate gRPC service for.</param>
        /// <param name="outputPath">The output file path.</param>
        /// <param name="namespaceName">The namespace name.</param>
        /// <returns>A tuple containing proto file path and service implementation code.</returns>
        public (string ProtoFile, string ServiceImplementation) GenerateGrpcService(EntityStructure entity, 
            string outputPath, string namespaceName)
        {
            if (entity == null)
            {
                _helper.LogMessage("Serverless", "Entity cannot be null.", Errors.Failed);
                return (string.Empty, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(entity.EntityName))
            {
                _helper.LogMessage("Serverless", "Entity name cannot be empty.", Errors.Failed);
                return (string.Empty, string.Empty);
            }

            try
            {
                // Generate proto file
                StringBuilder protoSb = new StringBuilder();
                protoSb.AppendLine("syntax = \"proto3\";");
                protoSb.AppendLine();
                protoSb.AppendLine($"package {namespaceName.ToLower()};");
                protoSb.AppendLine();
                protoSb.AppendLine($"service {entity.EntityName}Service {{");
                protoSb.AppendLine($"  rpc Get({entity.EntityName}Request) returns ({entity.EntityName}Response);");
                protoSb.AppendLine($"  rpc Create({entity.EntityName}CreateRequest) returns ({entity.EntityName}Response);");
                protoSb.AppendLine($"  rpc Update({entity.EntityName}UpdateRequest) returns ({entity.EntityName}Response);");
                protoSb.AppendLine($"  rpc Delete({entity.EntityName}DeleteRequest) returns (EmptyResponse);");
                protoSb.AppendLine("}");
                protoSb.AppendLine();
                protoSb.AppendLine($"message {entity.EntityName} {{");

                int fieldNumber = 1;
                if (entity.Fields != null)
                {
                    foreach (var field in entity.Fields)
                    {
                        if (field != null && !string.IsNullOrWhiteSpace(field.FieldName))
                        {
                            string protoType = MapToProtoType(field.Fieldtype);
                            protoSb.AppendLine($"  {protoType} {field.FieldName.ToLower()} = {fieldNumber};");
                            fieldNumber++;
                        }
                    }
                }
                protoSb.AppendLine("}");
                protoSb.AppendLine();
                protoSb.AppendLine($"message {entity.EntityName}Request {{");
                protoSb.AppendLine("  string id = 1;");
                protoSb.AppendLine("}");
                protoSb.AppendLine();
                protoSb.AppendLine($"message {entity.EntityName}Response {{");
                protoSb.AppendLine($"  {entity.EntityName} data = 1;");
                protoSb.AppendLine("  string message = 2;");
                protoSb.AppendLine("}");
                protoSb.AppendLine();
                protoSb.AppendLine($"message {entity.EntityName}CreateRequest {{");
                protoSb.AppendLine($"  {entity.EntityName} data = 1;");
                protoSb.AppendLine("}");
                protoSb.AppendLine();
                protoSb.AppendLine($"message {entity.EntityName}UpdateRequest {{");
                protoSb.AppendLine("  string id = 1;");
                protoSb.AppendLine($"  {entity.EntityName} data = 2;");
                protoSb.AppendLine("}");
                protoSb.AppendLine();
                protoSb.AppendLine($"message {entity.EntityName}DeleteRequest {{");
                protoSb.AppendLine("  string id = 1;");
                protoSb.AppendLine("}");
                protoSb.AppendLine();
                protoSb.AppendLine("message EmptyResponse {}");

                string protoCode = protoSb.ToString();

                // Generate C# service implementation
                StringBuilder serviceSb = new StringBuilder();
                serviceSb.AppendLine("using System;");
                serviceSb.AppendLine("using System.Threading.Tasks;");
                serviceSb.AppendLine("using Grpc.Core;");
                serviceSb.AppendLine();
                serviceSb.AppendLine($"namespace {namespaceName}");
                serviceSb.AppendLine("{");
                serviceSb.AppendLine($"    /// <summary>");
                serviceSb.AppendLine($"    /// gRPC service implementation for {entity.EntityName}");
                serviceSb.AppendLine($"    /// </summary>");
                serviceSb.AppendLine($"    public class {entity.EntityName}Service : {entity.EntityName}ServiceBase");
                serviceSb.AppendLine("    {");
                serviceSb.AppendLine($"        private readonly IDMEEditor _dmeEditor;");
                serviceSb.AppendLine();
                serviceSb.AppendLine($"        public {entity.EntityName}Service(IDMEEditor dmeEditor)");
                serviceSb.AppendLine("        {");
                serviceSb.AppendLine("            _dmeEditor = dmeEditor ?? throw new ArgumentNullException(nameof(dmeEditor));");
                serviceSb.AppendLine("        }");
                serviceSb.AppendLine();
                serviceSb.AppendLine($"        public override Task<{entity.EntityName}Response> Get({entity.EntityName}Request request, ServerCallContext context)");
                serviceSb.AppendLine("        {");
                serviceSb.AppendLine("            try");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine($"                if (string.IsNullOrEmpty(request.Id))");
                serviceSb.AppendLine($"                    throw new RpcException(new Status(StatusCode.InvalidArgument, \"ID is required\"));");
                serviceSb.AppendLine();
                serviceSb.AppendLine($"                var response = new {entity.EntityName}Response");
                serviceSb.AppendLine("                {");
                serviceSb.AppendLine($"                    Data = new {entity.EntityName}(),");
                serviceSb.AppendLine($"                    Message = \"Record retrieved successfully\"");
                serviceSb.AppendLine("                };");
                serviceSb.AppendLine();
                serviceSb.AppendLine("                return Task.FromResult(response);");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("            catch (Exception ex)");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine("                throw new RpcException(new Status(StatusCode.Internal, ex.Message));");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("        }");
                serviceSb.AppendLine();
                serviceSb.AppendLine($"        public override Task<{entity.EntityName}Response> Create({entity.EntityName}CreateRequest request, ServerCallContext context)");
                serviceSb.AppendLine("        {");
                serviceSb.AppendLine("            try");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine($"                if (request?.Data == null)");
                serviceSb.AppendLine($"                    throw new RpcException(new Status(StatusCode.InvalidArgument, \"Data is required\"));");
                serviceSb.AppendLine();
                serviceSb.AppendLine($"                var uow = _dmeEditor.CreateUnitOfWork<{entity.EntityName}>();");
                serviceSb.AppendLine("                uow.AddNew(request.Data);");
                serviceSb.AppendLine("                uow.Commit();");
                serviceSb.AppendLine();
                serviceSb.AppendLine($"                var response = new {entity.EntityName}Response");
                serviceSb.AppendLine("                {");
                serviceSb.AppendLine($"                    Data = request.Data,");
                serviceSb.AppendLine($"                    Message = \"Record created successfully\"");
                serviceSb.AppendLine("                };");
                serviceSb.AppendLine();
                serviceSb.AppendLine("                return Task.FromResult(response);");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("            catch (Exception ex)");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine("                throw new RpcException(new Status(StatusCode.Internal, ex.Message));");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("        }");
                serviceSb.AppendLine();
                serviceSb.AppendLine($"        public override Task<{entity.EntityName}Response> Update({entity.EntityName}UpdateRequest request, ServerCallContext context)");
                serviceSb.AppendLine("        {");
                serviceSb.AppendLine("            try");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine($"                if (string.IsNullOrEmpty(request.Id) || request?.Data == null)");
                serviceSb.AppendLine($"                    throw new RpcException(new Status(StatusCode.InvalidArgument, \"ID and Data are required\"));");
                serviceSb.AppendLine();
                serviceSb.AppendLine($"                var uow = _dmeEditor.CreateUnitOfWork<{entity.EntityName}>();");
                serviceSb.AppendLine("                uow.Modify(request.Data);");
                serviceSb.AppendLine("                uow.Commit();");
                serviceSb.AppendLine();
                serviceSb.AppendLine($"                var response = new {entity.EntityName}Response");
                serviceSb.AppendLine("                {");
                serviceSb.AppendLine($"                    Data = request.Data,");
                serviceSb.AppendLine($"                    Message = \"Record updated successfully\"");
                serviceSb.AppendLine("                };");
                serviceSb.AppendLine();
                serviceSb.AppendLine("                return Task.FromResult(response);");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("            catch (Exception ex)");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine("                throw new RpcException(new Status(StatusCode.Internal, ex.Message));");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("        }");
                serviceSb.AppendLine();
                serviceSb.AppendLine($"        public override Task<EmptyResponse> Delete({entity.EntityName}DeleteRequest request, ServerCallContext context)");
                serviceSb.AppendLine("        {");
                serviceSb.AppendLine("            try");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine($"                if (string.IsNullOrEmpty(request.Id))");
                serviceSb.AppendLine($"                    throw new RpcException(new Status(StatusCode.InvalidArgument, \"ID is required\"));");
                serviceSb.AppendLine();
                serviceSb.AppendLine($"                // Implementation would delete record based on ID");
                serviceSb.AppendLine("                var response = new EmptyResponse();");
                serviceSb.AppendLine("                return Task.FromResult(response);");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("            catch (Exception ex)");
                serviceSb.AppendLine("            {");
                serviceSb.AppendLine("                throw new RpcException(new Status(StatusCode.Internal, ex.Message));");
                serviceSb.AppendLine("            }");
                serviceSb.AppendLine("        }");
                serviceSb.AppendLine("    }");
                serviceSb.AppendLine("}");

                string serviceCode = serviceSb.ToString();

                // Write proto file
                string protoFilePath = null;
                string serviceFilePath = null;

                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    _helper.EnsureOutputDirectory(outputPath);
                    var protoPath = Path.Combine(outputPath, $"{entity.EntityName}.proto");
                    var servicePath = Path.Combine(outputPath, $"{entity.EntityName}Service.cs");
                    var protoOk = _helper.WriteToFile(protoPath, protoCode, entity.EntityName);
                    var serviceOk = _helper.WriteToFile(servicePath, serviceCode, entity.EntityName);
                    if (protoOk || serviceOk)
                    {
                        _helper.LogMessage("Serverless", $"gRPC service generated: {protoPath}, {servicePath}");
                    }
                    protoFilePath = protoOk ? protoPath : null;
                    serviceFilePath = serviceOk ? servicePath : null;
                }

                return (protoFilePath ?? protoCode, serviceFilePath ?? serviceCode);
            }
            catch (Exception ex)
            {
                _helper.LogMessage("Serverless", $"Error generating gRPC service: {ex.Message}", Errors.Failed);
                return (string.Empty, string.Empty);
            }
        }

        /// <summary>
        /// Maps database field type to Protocol Buffers type.
        /// </summary>
        private string MapToProtoType(string Fieldtype)
        {
            if (string.IsNullOrEmpty(Fieldtype))
                return "string";

            string lowerType = Fieldtype.ToLower();

            if (lowerType.Contains("int"))
                return "int32";
            if (lowerType.Contains("long"))
                return "int64";
            if (lowerType.Contains("decimal") || lowerType.Contains("double"))
                return "double";
            if (lowerType.Contains("float"))
                return "float";
            if (lowerType.Contains("bool"))
                return "bool";
            if (lowerType.Contains("date") || lowerType.Contains("time"))
                return "string"; // Use string for dates in protobuf
            if (lowerType.Contains("guid"))
                return "string";
            if (lowerType.Contains("byte"))
                return "bytes";

            return "string";
        }
    }
}
