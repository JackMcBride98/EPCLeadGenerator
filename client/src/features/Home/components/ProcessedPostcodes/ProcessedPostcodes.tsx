import { searchProcessedPostcodesOptions } from "@api/@tanstack/react-query.gen.ts";
import { ProcessedPostcodesResults } from "@features/Home/components/ProcessedPostcodes/ProcessedPostcodesResults.tsx";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";

export const ProcessedPostcodes = () => {
  const [postcodeSearchTerm, setPostcodeSearchTerm] = useState("");

  const { data, isPending, isError, error } = useQuery({
    ...searchProcessedPostcodesOptions({
      query: { postcodeSearchTerm },
    }),
  });

  return (
    <div className="mx-auto w-full space-y-4 p-4">
      <PostcodeSearchInput
        value={postcodeSearchTerm}
        onChange={setPostcodeSearchTerm}
      />
      <ProcessedPostcodesResults
        isLoading={isPending}
        isError={isError}
        error={error}
        postcodes={data?.postcodes ?? []}
        searchTerm={postcodeSearchTerm}
      />
    </div>
  );
};

interface PostcodeSearchInputProps {
  value: string;
  onChange: (value: string) => void;
}

export const PostcodeSearchInput = ({
  value,
  onChange,
}: PostcodeSearchInputProps) => (
  <div className="flex flex-col gap-2">
    <label
      htmlFor="postcode-search"
      className="text-sm font-semibold text-gray-700"
    >
      Search Processed Postcodes
    </label>
    <input
      id="postcode-search"
      type="text"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder="Search postcodes (e.g. SW1A)..."
      className="focus:border-link-green focus:ring-link-green w-80 rounded-md border border-gray-300 px-4 py-2 text-sm shadow-sm focus:ring-1 focus:outline-none"
    />
  </div>
);
