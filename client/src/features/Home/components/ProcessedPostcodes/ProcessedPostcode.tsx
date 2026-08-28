import type { SearchProcessedPostcodesPostcodeResponse } from "@api/types.gen.ts";
import { EPCAggregationDisplay } from "@features/Home/components/ProcessedPostcodes/EPCAggregationDisplay.tsx";
import { EPCAssessments } from "@features/Home/components/ProcessedPostcodes/EPCAssessments.tsx";
import { MarkAsDoneCheckbox } from "@features/Home/components/ProcessedPostcodes/MarkAsDoneCheckbox.tsx";
import { formatDate } from "@helpers/dateHelpers.ts";
import { useState } from "react";
import { LSOADeprivationDisplay } from "./LSOADeprivationDisplay.tsx";

interface ProcessedPostcodeItemProps {
  postcode: SearchProcessedPostcodesPostcodeResponse;
}

export const ProcessedPostcode = ({ postcode }: ProcessedPostcodeItemProps) => {
  const [isExpanded, setIsExpanded] = useState(false);
  return (
    <>
      <li className="flex gap-3 p-4 transition-colors hover:bg-gray-50/50">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-base leading-none font-bold text-gray-900">
              {postcode.postcode}
            </h3>
            <p className="mt-1 text-xs text-gray-500">
              EPCs last fetched at {formatDate(postcode.epCsLastUpdatedAt)}
            </p>
          </div>
        </div>

        <LSOADeprivationDisplay deprivation={postcode.lsoaDeprivation} />

        <EPCAggregationDisplay aggregation={postcode.epcAggregation} />

        <button
          type="button"
          onClick={() => setIsExpanded((prev) => !prev)}
          className="rounded border border-gray-200 bg-white px-2.5 py-1 text-xs font-medium text-gray-700 shadow-sm transition-colors hover:bg-gray-50"
        >
          {isExpanded ? "Hide Details" : "More Details"}
        </button>

        <MarkAsDoneCheckbox postcode={postcode.postcode} isDone={false} />
      </li>
      {isExpanded && (
        <EPCAssessments assessments={postcode.epcAssessments ?? []} />
      )}
    </>
  );
};
