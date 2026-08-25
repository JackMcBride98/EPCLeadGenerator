import { getUserOptions } from "@api/@tanstack/react-query.gen.ts";
import { client } from "@api/client.gen.ts";
import { useQuery } from "@tanstack/react-query";
import { SpinnerCircularFixed } from "spinners-react";

export const Profile = () => {
  const { isLoading, isError, error, isSuccess, data } = useQuery({
    ...getUserOptions({ client }),
  });

  if (isLoading) {
    return (
      <div className="flex h-full min-h-screen w-full min-w-screen flex-col items-center space-y-4 bg-black text-white">
        <SpinnerCircularFixed color="#7c3aed" />
      </div>
    );
  }

  if (isError || !isSuccess) {
    return (
      <div className="flex h-full min-h-screen w-full min-w-screen flex-col items-center space-y-4 bg-black text-white">
        <p className="text-red-600">Error: {error?.title ?? "Unknown error"}</p>{" "}
        <p className="text-red-600">{error?.detail}</p>
        <a href="/" className="text-violet-600 hover:underline">
          Go back to the home page
        </a>
      </div>
    );
  }

  const { user } = data;

  return (
    <div className="flex h-full min-h-screen w-full min-w-screen flex-col items-center space-y-4 bg-black text-white">
      <p>{user.userId}</p>
      <p>{user.name}</p>
      <p>{user.createdAt}</p>
    </div>
  );
};
