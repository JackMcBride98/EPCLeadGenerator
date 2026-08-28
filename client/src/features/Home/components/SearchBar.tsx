import searchIcon from "@assets/search.svg";
import { useState } from "react";

interface SearchBarProps {
  onSearch: (searchTerm: string) => void;
  isLoading: boolean;
}

export const SearchBar = ({ onSearch, isLoading }: SearchBarProps) => {
  const [intermediateSearchTerm, setIntermediateSearchTerm] = useState("");

  const handleSearch = () => {
    onSearch(intermediateSearchTerm);
  };

  return (
    <>
      <div className="bg-primary flex items-center rounded-md pl-1">
        <input
          className="w-full overflow-visible bg-white p-2 outline-0"
          type="text"
          placeholder="Enter a valid UK postcode e.g. BS7 8PU"
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              handleSearch();
            }
          }}
          value={intermediateSearchTerm}
          onChange={(e) => setIntermediateSearchTerm(e.target.value)}
        />
        <button
          disabled={isLoading}
          className="p-4 disabled:cursor-not-allowed"
          onClick={handleSearch}
        >
          <img
            src={searchIcon}
            alt="search"
            className="bg-primary-600 h-6 w-6"
          />
        </button>
      </div>
    </>
  );
};
