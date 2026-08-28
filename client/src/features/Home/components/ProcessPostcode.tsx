import { processPostcodeMutation } from "@api/@tanstack/react-query.gen.ts";
import { SearchBar } from "@features/Home/components/SearchBar.tsx";
import { useMutation } from "@tanstack/react-query";

export const ProcessPostcode = () => {
  const { mutate, isPending, isError, error, data } = useMutation({
    ...processPostcodeMutation(),
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
