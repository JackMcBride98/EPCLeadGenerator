import { ChangeEvent, FC } from "react";

interface MarkAsDoneCheckboxProps {
  postcode: string;
  isDone: boolean;
}

export const MarkAsDoneCheckbox: FC<MarkAsDoneCheckboxProps> = ({
  postcode,
  isDone,
}) => {
  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    e.preventDefault();
    alert(
      `"Mark as Done" functionality is not implemented yet. Let me know if you would want this.
       I was imagining done as meaning you had flyered the area.`,
    );
  };

  return (
    <div className="flex items-center gap-2 border-l border-gray-200 pl-4">
      <label
        htmlFor={`mark-done-${postcode}`}
        className="flex cursor-pointer items-center gap-2 text-xs font-medium text-gray-600 select-none hover:text-gray-900"
      >
        <input
          id={`mark-done-${postcode}`}
          type="checkbox"
          checked={isDone}
          onChange={handleChange}
          className="text-link-green focus:ring-link-green h-4 w-4 cursor-pointer rounded border-gray-300"
        />
        <span>Done</span>
      </label>
    </div>
  );
};
