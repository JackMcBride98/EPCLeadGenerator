import type {
  ProblemDetails,
  SearchProcessedPostcodesPostcodeResponse,
} from "@api/types.gen.ts";
import { formatDate } from "@helpers/dateHelpers.ts";

interface ProcessedPostcodeItemProps {
  postcode: SearchProcessedPostcodesPostcodeResponse;
}

export const ProcessedPostcode = ({ postcode }: ProcessedPostcodeItemProps) => {
  return (
    <li className="flex items-center justify-between p-4 transition-colors hover:bg-gray-50">
      <div>
        <p className="font-semibold text-gray-900">{postcode.postcode}</p>
        <p className="text-xs text-gray-500">
          {formatDate(postcode.epCsLastUpdatedAt)}
        </p>
      </div>
    </li>
  );
};

interface ProcessedPostcodesListProps {
  isLoading: boolean;
  isError: boolean;
  error: ProblemDetails | null;
  postcodes: Array<SearchProcessedPostcodesPostcodeResponse>;
  searchTerm: string;
}

export const ProcessedPostcodesList = ({
  isLoading,
  isError,
  error,
  postcodes,
  searchTerm,
}: ProcessedPostcodesListProps) => {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="border-link-green h-6 w-6 animate-spin rounded-full border-2 border-t-transparent" />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="rounded-md bg-red-50 p-3 text-sm text-red-700">
        Failed to load postcodes: {error!.title || "Something went wrong"}
        <br />
        {error!.detail}
      </div>
    );
  }

  if (postcodes.length === 0) {
    return (
      <p className="py-6 text-center text-sm text-gray-500">
        No processed postcodes found matching "{searchTerm}".
      </p>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-gray-200 shadow-sm">
      <ul className="divide-y divide-gray-200 bg-white">
        {postcodes.map((postcode) => (
          <ProcessedPostcode key={postcode.postcode} postcode={postcode} />
        ))}
      </ul>
    </div>
  );
};
