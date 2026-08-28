import type { SearchProcessedPostcodesEpcAssessmentAggregationResponse } from "@api/types.gen.ts";

interface EPCAggregationDisplayProps {
  aggregation: SearchProcessedPostcodesEpcAssessmentAggregationResponse;
}

// Color coding mapping for standard UK EPC Bands
const getEpcBadgeStyle = (rating: string, percentage: number) => {
  if (percentage === 0) {
    return "bg-gray-50 text-gray-400 border-gray-200 opacity-60";
  }

  switch (rating.toUpperCase()) {
    case "A":
    case "B":
      return "bg-green-100 text-green-800 border-green-200";
    case "C":
      return "bg-emerald-100 text-emerald-800 border-emerald-200";
    case "D":
      return "bg-yellow-100 text-yellow-800 border-yellow-200";
    case "E":
      return "bg-amber-100 text-amber-800 border-amber-200";
    case "F":
      return "bg-orange-100 text-orange-800 border-orange-200";
    case "G":
      return "bg-red-100 text-red-800 border-red-200";
    default:
      return "bg-gray-100 text-gray-800 border-gray-200";
  }
};

export const EPCAggregationDisplay = ({
  aggregation,
}: EPCAggregationDisplayProps) => {
  return (
    <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-xs">
      {/* Total Assessments */}
      <div className="flex flex-col items-center gap-1">
        <span className="font-medium text-gray-500">Total:</span>
        <span className="font-bold text-gray-900">
          {aggregation.totalAssessments}
        </span>
      </div>

      {/* Expired % */}
      <div className="flex flex-col items-center gap-1">
        <span className="font-medium text-gray-500">Expired:</span>
        <span className="font-bold text-red-600">
          {aggregation.percentageExpired.toFixed(1)}%
        </span>
      </div>

      {/* Expiring Soon % */}
      <div className="flex flex-col items-center gap-1">
        <span className="font-medium text-gray-500">Expiring 1yr:</span>
        <span className="font-bold text-amber-600">
          {aggregation.percentageExpiringInNextYear.toFixed(1)}%
        </span>
      </div>

      {/* Rating Breakdown Pill Bar (Includes 0%) */}
      <div className="flex flex-col items-center gap-1">
        <span className="font-medium text-gray-500">Ratings:</span>
        <div className="flex items-center gap-1">
          {Object.entries(aggregation.epcRatingPercentages ?? {}).map(
            ([rating, percentage]) => (
              <span
                key={rating}
                className={`inline-flex items-center rounded border px-1.5 py-0.5 text-[10px] font-bold ${getEpcBadgeStyle(
                  rating,
                  percentage,
                )}`}
                title={`${rating}: ${percentage}%`}
              >
                {rating}: {percentage.toFixed(0)}%
              </span>
            ),
          )}
        </div>
      </div>
    </div>
  );
};
