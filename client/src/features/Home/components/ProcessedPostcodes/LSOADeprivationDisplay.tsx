import type { SearchProcessedPostcodesLsoaDeprivationResponse } from "@api/types.gen.ts";

interface LSOADeprivationDisplayProps {
  deprivation: SearchProcessedPostcodesLsoaDeprivationResponse;
}

const getOrdinal = (n: number): string => {
  const lastTwo = n % 100;
  if (lastTwo >= 11 && lastTwo <= 13) return `${n.toLocaleString()}th`;
  switch (n % 10) {
    case 1:
      return `${n.toLocaleString()}st`;
    case 2:
      return `${n.toLocaleString()}nd`;
    case 3:
      return `${n.toLocaleString()}rd`;
    default:
      return `${n.toLocaleString()}th`;
  }
};

export const LSOADeprivationDisplay = ({
  deprivation,
}: LSOADeprivationDisplayProps) => {
  const metrics = [
    {
      label: "Multiple",
      rank: deprivation.multipleDeprivationRank,
      percentage: deprivation.multipleDeprivationPercentage,
    },
    {
      label: "Income",
      rank: deprivation.incomeDeprivationRank,
      percentage: deprivation.incomeDeprivationPercentage,
    },
    {
      label: "Employment",
      rank: deprivation.employmentDeprivationRank,
      percentage: deprivation.employmentDeprivationPercentage,
    },
    {
      label: "Housing & Services",
      rank: deprivation.barriersToHousingAndServicesRank,
      percentage: deprivation.barriersToHousingAndServicesPercentage,
    },
  ];

  return (
    <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-xs">
      {metrics.map((metric) => (
        <div key={metric.label} className="flex flex-col items-center gap-1.5">
          <span className="font-medium text-gray-500">{metric.label}:</span>
          <span className="font-semibold text-gray-900">
            {getOrdinal(metric.rank)}
          </span>
          <span className="text-dark-green font-medium">
            ({metric.percentage.toFixed(1)}%)
          </span>
        </div>
      ))}
    </div>
  );
};
