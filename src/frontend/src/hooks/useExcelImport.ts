import { useState, useCallback, useMemo, useEffect } from 'react';
import { useAppContext } from '../appContext';
import ExcelParseReponse, { ExcelHeader } from '../models/ExcelParseResponse';

export type autoGenerateColumn<T> = {
    title: keyof T;
    format?: (item: string) => {};
};

export type Column<T> = {
    title: keyof T;
    variations?: string[];
    format?: (item: string) => {};
};

export type ExcelImportSettings<T> = {
    columns: Column<T>[];
    autoGenerateColumns?: autoGenerateColumn<T>[];
};

type ColumnIndex<T> = {
    [key in keyof T]: number;
};

const useExcelImport = <T>(excelImportSettings: ExcelImportSettings<T>) => {
    const { apiClient } = useAppContext();

    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [isProccessingFile, setIsProccessingFile] = useState<boolean>(false);
    const [processedFile, setProcessedFile] = useState<T[] | null>(null);
    const [error, setError] = useState<boolean>(false);
    const { columns, autoGenerateColumns } = excelImportSettings;

    useEffect(() => {
        if (selectedFile === null) return;

        setIsProccessingFile(true);
        getExcelReponseAsync(selectedFile);
    }, [selectedFile]);

    const getExcelReponseAsync = async (file: File) => {
        const excelReponse = await apiClient.ExcelImportParserAsync(file);
        if (excelReponse) formatExcelReponse(excelReponse);
        setIsProccessingFile(false);
    };

    const mapHeaderIndexesToColumns = (headers: ExcelHeader[]): ColumnIndex<T> => {
        let columnIndexes = {};

        for (const column of columns) {
            const header = headers.find((h) =>
                column.variations
                    ? column.variations.includes(h.title.toLocaleLowerCase())
                    : h.title.toLocaleLowerCase() === column.title.toString().toLocaleLowerCase()
            );

            columnIndexes = {
                ...columnIndexes,
                [column.title]: header?.colIndex || header?.colIndex === 0 ? header.colIndex : -1,
            };
        }

        return columnIndexes as ColumnIndex<T>;
    };

    const formatExcelReponse = (response: ExcelParseReponse) => {
        const { headers, data } = response;

        const columnIndexes = mapHeaderIndexesToColumns(headers);

        const mappedReponse: T[] = data.map((row) => {
            const { items } = row;
            let mappedRow = {};

            for (const column of columns) {
                const index = columnIndexes[column.title];
                const value = index >= 0 ? items[index] : '';
                const formattedValue = column.format ? column.format(value) : value;

                mappedRow = { ...mappedRow, [column.title]: formattedValue };
            }

            return mappedRow as T;
        });

        console.log(mappedReponse);
        setProcessedFile(mappedReponse);
        setIsProccessingFile(false);
    };

    return {
        setSelectedFile,
        isProccessingFile,
        processedFile,
        error,
    };
};

export default useExcelImport;
