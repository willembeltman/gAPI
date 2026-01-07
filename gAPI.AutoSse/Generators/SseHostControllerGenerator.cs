namespace gAPI.AutoSse.Generators
{
    internal class SseHostControllerGenerator : BaseGenerator
    {
        internal SseHostControllerGenerator(ServiceContext dataModel)
        {
            DataModel = dataModel;

            Directory = dataModel.Config.SseServices_Destination.Directory;
            Namespace = dataModel.Config.SseServices_Destination.Namespace;

            Name = "SseHostController";
            FileName = $"{Name}.g.cs";
        }

        public ServiceContext DataModel { get; }

        public void GenerateCode()
        {
            Code = "";
            return;
        }
    }
}