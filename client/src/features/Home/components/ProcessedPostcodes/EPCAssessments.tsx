import type { SearchProcessedPostcodesEpcAssessmentResponse } from "@api/types.gen.ts";
import { formatDate } from "@helpers/dateHelpers.ts";

interface EPCAssessmentsProps {
  assessments: SearchProcessedPostcodesEpcAssessmentResponse[];
}

const getRatingBadgeStyle = (rating: string | null) => {
  if (!rating) return "bg-gray-100 text-gray-700 border-gray-200";

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

export const EPCAssessments = ({ assessments }: EPCAssessmentsProps) => {
  if (!assessments || assessments.length === 0) {
    return (
      <div className="p-4 text-center text-xs text-gray-500">
        No EPC assessments recorded for this postcode.
      </div>
    );
  }

  return (
    <div className="border-t border-gray-100 bg-gray-50/50 p-4">
      <div className="overflow-x-auto rounded-lg border border-gray-200 bg-white shadow-sm">
        <table className="w-full text-left text-xs">
          <thead className="border-b border-gray-200 bg-gray-50 font-semibold text-gray-600">
            <tr>
              <th className="px-3 py-2">Address</th>
              <th className="px-3 py-2">Rating</th>
              <th className="px-3 py-2">UPRN</th>
              <th className="px-3 py-2">Certificate No.</th>
              <th className="px-3 py-2">Registered</th>
              <th className="px-3 py-2">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {assessments.map((item) => (
              <tr
                key={item.epcAssessmentId}
                className="transition-colors hover:bg-gray-50/80"
              >
                <td className="px-3 py-2 font-medium text-gray-900">
                  {item.addressLine}
                </td>
                <td className="px-3 py-2">
                  <span
                    className={`inline-flex items-center rounded border px-2 py-0.5 text-[10px] font-bold ${getRatingBadgeStyle(
                      item.epcRating,
                    )}`}
                  >
                    {item.epcRating ?? "N/A"}
                  </span>
                </td>
                <td className="px-3 py-2 font-mono text-gray-500">
                  {item.uniquePropertyReferenceNumber}
                </td>
                <td className="px-3 py-2 font-mono text-gray-500">
                  {item.certificateNumber}
                </td>
                <td className="px-3 py-2 text-gray-600">
                  {formatDate(item.registrationDate)}
                </td>
                <td className="px-3 py-2">
                  <div className="flex items-center gap-1">
                    {item.isLatest && (
                      <span className="rounded border border-blue-200 bg-blue-50 px-1.5 py-0.5 text-[10px] font-medium text-blue-700">
                        Latest
                      </span>
                    )}
                    {item.isExpired && (
                      <span className="rounded border border-red-200 bg-red-50 px-1.5 py-0.5 text-[10px] font-medium text-red-700">
                        Expired
                      </span>
                    )}
                    {item.isExpiringInNextYear && !item.isExpired && (
                      <span className="rounded border border-amber-200 bg-amber-50 px-1.5 py-0.5 text-[10px] font-medium text-amber-700">
                        Expiring Soon
                      </span>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
