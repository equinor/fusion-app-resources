import { useState, useEffect } from 'react';
import { useAppContext } from '../appContext';
import ExcelParseReponse, { ExcelHeader } from '../models/ExcelParseResponse';

export type AutoGenerateColumn<T> = {
    title: keyof T;
    format: (columns: Column<T>[]) => {};
};

export type Column<T> = {
    title: keyof T;
    variations?: string[];
    format?: (item: string) => {};
};

export type ExcelImportSettings<T> = {
    columns: Column<T>[];
    autoGenerateColumns?: AutoGenerateColumn<T>[];
};

type ColumnIndex<T> = {
    [key in keyof T]: number;
};

const useExcelImport = <T>(excelImportSettings: ExcelImportSettings<T>) => {
    const { apiClient } = useAppContext();

    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [isProccessingFile, setIsProccessingFile] = useState<boolean>(false);
    const [processedFile, setProcessedFile] = useState<T[] | null>(null);
    const [processingError, setProcessingError] = useState<boolean>(false);
    const { columns, autoGenerateColumns } = excelImportSettings;

    useEffect(() => {
        if (selectedFile === null) return;

        processExcelFileAsync(selectedFile);
    }, [selectedFile]);

    const processExcelFileAsync = async (file: File) => {
        setIsProccessingFile(true);
        setProcessingError(false);

        try {
            const excelReponse = await apiClient.parseExcelFileAsync(file);

            if (excelReponse) {
                const formattedResponse = formatExcelReponse(excelReponse);
                setProcessedFile(formattedResponse);
            } else {
                setProcessedFile(null);
            }
        } catch (e) {
            setProcessingError(true);
        } finally {
            setIsProccessingFile(false);
        }
    };

    const formatExcelReponse = (response: ExcelParseReponse) => {
        const { headers, data } = response;
        const columnIndexes = mapHeaderIndexesToColumns(headers);

        return data.map((row) => {
            const { items } = row;

            const mappedRow = columns.reduce((row, column) => {
                const index = columnIndexes[column.title];
                const value = index >= 0 ? items[index] : '';
                const formattedValue = column.format ? column.format(value) : value;

                return { ...row, [column.title]: formattedValue };
            }, {} as T);

            if (autoGenerateColumns) {
                const generatedColumns = autoGenerateColumns.reduce((row, column) => {
                    return { ...row, [column.title]: column.format(columns) };
                }, {} as T);

                return { ...mappedRow, ...generatedColumns };
            }

            return mappedRow;
        });
    };

    const mapHeaderIndexesToColumns = (headers: ExcelHeader[]): ColumnIndex<T> => {
        return columns.reduce((ci, column) => {
            return {
                ...ci,
                [column.title]: findHeaderIndex(headers, column),
            };
        }, {} as ColumnIndex<T>);
    };

    const findHeaderIndex = (headers: ExcelHeader[], column: Column<T>) => {
        const index = headers.find((h) =>
            column.variations
                ? column.variations.includes(h.title.toLowerCase())
                : h.title.toLowerCase() === column.title.toString().toLowerCase()
        )?.colIndex;

        return index || index === 0 ? index : -1;
    };

    return {
        setSelectedFile,
        isProccessingFile,
        processedFile,
        processingError,
    };
};

export default useExcelImport;
