type ReadableStreamResponse = {
    body: ReadableStream<Uint8Array>;
    bodyUsed: boolean;
    headers: any;
    ok: boolean;
    redirect: boolean;
    status: number;
    statusText: string;
    tpye: string;
    url: string;
};

export default ReadableStreamResponse;
