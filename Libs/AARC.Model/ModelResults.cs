namespace AARC.Model
{
    public class ModelResults
    {
        public double[] AssetPrices;	// final prices generated using the model
        public double[] AssetVols;		// these represent the vols that occurred during generation of prices using the model

        public double[] CallPrices;
        public double[] PutPrices;
    }
}