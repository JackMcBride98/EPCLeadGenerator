import type {
  ProblemDetails,
  SearchProcessedPostcodesPostcodeResponse,
} from "@api/types.gen.ts";
import { ProcessedPostcode } from "@features/Home/components/ProcessedPostcodes/ProcessedPostcode.tsx";

interface ProcessedPostcodesListProps {
  isLoading: boolean;
  isError: boolean;
  error: ProblemDetails | null;
  postcodes: Array<SearchProcessedPostcodesPostcodeResponse>;
  searchTerm: string;
}

export const ProcessedPostcodesResults = ({
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
    <div className="w-full overflow-hidden rounded-lg border border-gray-200 shadow-sm">
      <ul className="divide-y divide-gray-200 bg-white">
        {postcodes.map((postcode) => (
          <ProcessedPostcode key={postcode.postcode} postcode={postcode} />
        ))}
      </ul>
    </div>
  );
};
