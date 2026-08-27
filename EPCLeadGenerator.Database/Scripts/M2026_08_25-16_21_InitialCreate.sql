CREATE TABLE "LSOADeprivation" (
    "LSOACode" VARCHAR(20) PRIMARY KEY,
    "LSOAName" VARCHAR(150) NOT NULL,
    "MultipleDeprivationRank" INT NOT NULL,
    "MultipleDeprivationDecile" INT NOT NULL,
    "MultipleDeprivationPercentage" NUMERIC(5,2) NOT NULL,
    "IncomeRank" INT NOT NULL,
    "IncomeDecile" INT NOT NULL,
    "IncomePercentage" NUMERIC(5,2) NOT NULL,
    "EmploymentRank" INT NOT NULL,
    "EmploymentDecile" INT NOT NULL,
    "EmploymentPercentage" NUMERIC(5,2) NOT NULL,
    "BarriersToHousingAndServicesRank" INT NOT NULL,
    "BarriersToHousingAndServicesDecile" INT NOT NULL,
    "BarriersToHousingAndServicesPercentage" NUMERIC(5,2) NOT NULL
);


CREATE TABLE "Postcodes" (
    "Postcode" VARCHAR(10) PRIMARY KEY,
    "LSOACode" VARCHAR(20) NULL,
    "MarkAsDone" BOOLEAN NOT NULL DEFAULT FALSE,
    "EPCsLastUpdatedAt" TIMESTAMPTZ NULL,
    CONSTRAINT "FkPostcodeLSOA" FOREIGN KEY ("LSOACode")
       REFERENCES "LSOADeprivation"("LSOACode")
);

CREATE INDEX "IdxPostcodesLSOACode" ON "Postcodes"("LSOACode");


CREATE TABLE "EPCAssessments" (
    "EPCAssessmentId" BIGSERIAL PRIMARY KEY,
    "UniquePropertyReferenceNumber" BIGINT NOT NULL,
    "CertificateNumber" VARCHAR(24) NOT NULL,
    "Postcode" VARCHAR(10) NOT NULL,
    "AddressLine" VARCHAR(1000) NOT NULL,
    "EPCRating" VARCHAR(2) CHECK ("EPCRating" IN ('A', 'B', 'C', 'D', 'E', 'F', 'G')),
    "IsLatest" BOOLEAN NOT NULL DEFAULT FALSE,
    "RegistrationDate" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FkEPCPostcode" FOREIGN KEY ("Postcode")
      REFERENCES "Postcodes"("Postcode")
);

CREATE UNIQUE INDEX "UqEPCCertificateNumber" ON "EPCAssessments"("CertificateNumber");

CREATE INDEX "IdxEPCUPRN" ON "EPCAssessments"("UniquePropertyReferenceNumber");

CREATE INDEX "IdxEPCPostcodeIsLatest" ON "EPCAssessments"("Postcode", "IsLatest");
