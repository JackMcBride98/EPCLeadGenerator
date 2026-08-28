import type { SearchProcessedPostcodesPostcodeResponse } from "@api/types.gen.ts";
import { formatDate } from "@helpers/dateHelpers.ts";
import { LSOADeprivationDisplay } from "./LSOADeprivationDisplay.tsx";

interface ProcessedPostcodeItemProps {
  postcode: SearchProcessedPostcodesPostcodeResponse;
}

export const ProcessedPostcode = ({ postcode }: ProcessedPostcodeItemProps) => {
  return (
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

      {postcode.lsoaDeprivation && (
        <LSOADeprivationDisplay deprivation={postcode.lsoaDeprivation} />
      )}
    </li>
  );
};
