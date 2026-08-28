import {
  processPostcodeMutation,
  searchProcessedPostcodesQueryKey,
} from "@api/@tanstack/react-query.gen.ts";
import { SearchBar } from "@features/Home/components/SearchBar.tsx";
import { useMutation, useQueryClient } from "@tanstack/react-query";

export const ProcessPostcode = () => {
  const queryClient = useQueryClient();
  const { mutate, isPending, isError, error, data } = useMutation({
    ...processPostcodeMutation(),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: searchProcessedPostcodesQueryKey({}),
      });
    },
  });

  const handleSearch = (postcode: string) => {
    mutate({
      body: { postcode: postcode },
    });
  };

  return (
    <div>
      <p>Process postcode</p>
      <SearchBar onSearch={handleSearch} isLoading={isPending} />

      {isPending && <p>Loading...</p>}
      {isError && (
        <p className="text-red-500">
          Error: {error.title} {error.detail}
        </p>
      )}
      {data && (
        <p className="text-link-green">
          {data.postcode} {data.message}
        </p>
      )}
    </div>
  );
};
