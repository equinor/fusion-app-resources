export type ExcelHeader = {
    cellRef: string;
    title: string;
    colIndex: number;
};

export type ExcelRow = {
    index: number;
    items: string[];
};

type ExcelParseReponse = {
    headers: ExcelHeader[];
    data: ExcelRow[];
    messages: string[];
};

export default ExcelParseReponse;
