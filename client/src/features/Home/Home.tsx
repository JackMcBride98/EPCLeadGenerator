import { Credits } from "./components/Credits.tsx";

export const Home = () => {
  return (
    <div className="flex h-screen w-screen flex-col items-center space-y-16 bg-black text-white">
      <h1 className="text-xl font-bold text-violet-600 md:text-3xl">
        Jack Template
      </h1>
      <p className="w-72 pb-5 md:w-80">
        JackTemplate insert app description here
      </p>
      <Credits />
    </div>
  );
};
