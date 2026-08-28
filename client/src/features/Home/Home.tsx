import { ProcessPostcode } from "@features/Home/components/ProcessPostcode.tsx";
import { ProcessedPostcodes } from "@features/Home/components/ProcessedPostcodes.tsx";
import { Credits } from "./components/Credits.tsx";

export const Home = () => {
  return (
    <div className="bg-dull flex h-screen w-screen flex-col items-center space-y-4 text-black">
      <h1 className="text-primary text-xl font-bold md:text-3xl">
        EPC Lead Generator
      </h1>
      <p className="pb-5">
        A tool which uses{" "}
        <a
          className="text-link-green"
          href="https://www.gov.uk/government/statistics/english-indices-of-deprivation-2025"
        >
          Deprivation Data
        </a>{" "}
        and the UK Government's{" "}
        <a
          className="text-link-green"
          href="https://get-energy-performance-data.communities.gov.uk/api-technical-documentation"
        >
          Energy certificate data API
        </a>{" "}
        to analyse given Postcodes suitability for energy performance
        certificates.
      </p>
      <ProcessPostcode />
      <ProcessedPostcodes />
      <Credits />
    </div>
  );
};
