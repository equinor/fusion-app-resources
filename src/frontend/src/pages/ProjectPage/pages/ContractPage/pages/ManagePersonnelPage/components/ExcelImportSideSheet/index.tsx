import * as React from 'react';
import * as styles from './styles.less';
import { Spinner, Button, ModalSideSheet } from '@equinor/fusion-components';
import { useAppContext } from '../../../../../../../../appContext';

type ExcelImportSideSheetProps = {
    setSelectedFile: React.Dispatch<React.SetStateAction<File | null>>;
    isProccessing: boolean;
    isOpen: boolean;
    setIsOpen: React.Dispatch<React.SetStateAction<boolean>>;
    processingError: boolean;
};

const ExcelImportSideSheet: React.FC<ExcelImportSideSheetProps> = ({
    setSelectedFile,
    isProccessing,
    isOpen,
    setIsOpen,
    processingError,
}) => {
    const [selectedFileForUpload, setSelectedFileForUpload] = React.useState<File | null>(null);
    const fileInput = React.useRef<HTMLInputElement>(null);
    const [fileError, setFileError] = React.useState<string | null>(null);

    const { apiClient } = useAppContext();

    React.useEffect(() => {
        setSelectedFileForUpload(null);
        setFileError(null);
    }, [isOpen]);

    const validateAndSetFile = React.useCallback((file: File) => {
        const fileExtension = file.name.substr(file.name.lastIndexOf('.') + 1).toLowerCase();

        if (fileExtension !== 'xlsx') {
            setFileError('Fileformat not supported. Please again with a xlsx file');
            setSelectedFileForUpload(null);
            return;
        }
        setFileError(null);
        setSelectedFileForUpload(file);
    }, []);

    const dragDropFileUpload = React.useCallback((e: React.DragEvent<HTMLDivElement>) => {
        e.preventDefault();
        if (!e.dataTransfer.items || e.dataTransfer.items[0].kind !== 'file') return;

        const file = e.dataTransfer.items[0].getAsFile();
        if (!file) return;
        validateAndSetFile(file);
    }, []);

    const inputFileUpload = React.useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        e.preventDefault();

        const file = e.target.files ? e.target.files[0] : null;
        if (!file) return;
        validateAndSetFile(file);
    }, []);

    const fileInputClick = React.useCallback(() => {
        fileInput?.current?.click();
    }, [fileInput]);

    const startProcessingSelectedFile = React.useCallback(() => {
        setSelectedFile(selectedFileForUpload);
    }, [selectedFileForUpload]);

    const stopPropagationAndDefault = React.useCallback((e: React.DragEvent<HTMLDivElement>) => {
        e.stopPropagation();
        e.preventDefault();
    }, []);

    const closeSidesheet = React.useCallback(() => {
        !isProccessing && setIsOpen(false);
    }, [isProccessing]);

    const downloadExcelTemplate = React.useCallback(async () => {
        const file = await apiClient.getPersonnelExcelTemplate();
        const reader = file.body.getReader();

        const readableStream = new ReadableStream({
            async start(controller) {
                while (true) {
                    const { done, value } = await reader.read();

                    if (done) break;

                    controller.enqueue(value);
                }

                controller.close();
                reader.releaseLock();
            },
        });

        new Response(readableStream)
            .blob()
            .then((blob) => URL.createObjectURL(blob))
            .then((url) => {
                const link = document.createElement('a');
                link.href = url;
                link.download = 'Personnel import.xlsx';
                link.click();
            });
    }, []);

    return (
        <ModalSideSheet
            header="Excel Import"
            show={isOpen}
            size={'medium'}
            onClose={closeSidesheet}
        >
            <div className={styles.container}>
                <div onClick={(e) => e.stopPropagation()}>
                    {isProccessing && <Spinner centered title="Processing Excel file" />}
                    {!isProccessing && (
                        <>
                            <div
                                className={styles.dragDropContainer}
                                onDrop={dragDropFileUpload}
                                onDragEnter={stopPropagationAndDefault}
                                onDragOver={stopPropagationAndDefault}
                            >
                                <div>
                                    <p>Drag and drop an excel file here</p>
                                    <p>or </p>
                                    <div className={styles.fileInput}>
                                        <div className={styles.inputButton}>
                                            <Button onClick={fileInputClick}>
                                                Select an excel file
                                            </Button>
                                        </div>
                                        <input
                                            className={styles.inputField}
                                            ref={fileInput}
                                            type="file"
                                            onChange={inputFileUpload}
                                        ></input>
                                    </div>
                                </div>
                                <div className={styles.errorMessageText}>
                                    {selectedFileForUpload &&
                                        !fileError &&
                                        `Selected file: ${selectedFileForUpload.name}`}
                                    {fileError && fileError}
                                </div>
                                <div className={styles.errorMessageText}>
                                    {processingError &&
                                        'Something went wrong, unable to process file'}
                                </div>
                            </div>
                            <div className={styles.processButton}>
                                <Button
                                    disabled={!selectedFileForUpload}
                                    onClick={startProcessingSelectedFile}
                                >
                                    Process selected excel file
                                </Button>
                            </div>
                            <div className={styles.help}>
                                <h3>Tips</h3>
                                <ul>
                                    <li>
                                        <Button outlined onClick={downloadExcelTemplate}>
                                            Get an empty excel template here
                                        </Button>
                                    </li>
                                    <li>
                                        Use the template to add information about the personnel you
                                        want to import
                                    </li>
                                    <li>
                                        Optional information can be left blank in the excel sheet
                                    </li>
                                    <li>
                                        After processing the excel sheet. You will get a preview of
                                        the data that is ready for import.
                                    </li>
                                    <li>
                                        You will be able to edit the data in the preview, before
                                        importing it.
                                    </li>
                                    <li>
                                        No data is saved before you press "save" in the preview. You
                                        can stop at any time before this.
                                    </li>
                                </ul>
                            </div>
                        </>
                    )}
                </div>
            </div>
        </ModalSideSheet>
    );
};

export default ExcelImportSideSheet;
