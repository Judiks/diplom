namespace object_recognition.Entities
{
    public class ImagePrediction : ImageInputData
    {
        public float[] Score;
        public float CurrentScore;

        public string PredictedLabelValue;
    }
}
