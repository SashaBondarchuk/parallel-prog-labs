import mmap
import struct
import logging
import os
import win32event
import win32api

folder = os.path.dirname(os.path.abspath(__file__))
logging.basicConfig(filename=f'{folder}/shm_log.txt', level=logging.INFO, encoding='utf-8')

def main():
    # Open shared memory
    shmem = mmap.mmap(-1, 8, "LabSharedMem")

    try:
        # Open named semaphores (they should already exist, created by C#)
        # SEMAPHORE_MODIFY_STATE = 0x0002
        SEMAPHORE_MODIFY_STATE = 0x0002
        semDataReady = win32event.OpenSemaphore(win32event.SYNCHRONIZE | SEMAPHORE_MODIFY_STATE, False, "LabSemDataReady")
        semDataRead = win32event.OpenSemaphore(win32event.SYNCHRONIZE | SEMAPHORE_MODIFY_STATE, False, "LabSemDataRead")
        semPythonReady = win32event.OpenSemaphore(win32event.SYNCHRONIZE | SEMAPHORE_MODIFY_STATE, False, "LabSemPythonReady")

        # Read iteration count
        iterations = struct.unpack('i', shmem[4:8])[0]
        logging.info(f"Starting {iterations} iterations")

        # Signal to C# that Python is ready
        win32event.ReleaseSemaphore(semPythonReady, 1)

        for i in range(iterations):
            # Wait for C# to signal that data is ready
            win32event.WaitForSingleObject(semDataReady, win32event.INFINITE)

            # Read the number from shared memory
            number = struct.unpack('i', shmem[0:4])[0]

            if (i + 1) % 100 == 0:
                logging.info(f"Processed {i + 1}/{iterations} iterations")

            # Signal to C# that we have read the data
            win32event.ReleaseSemaphore(semDataRead, 1)

        logging.info(f"Completed all {iterations} iterations")

        # Close semaphore handles
        win32api.CloseHandle(semDataReady)
        win32api.CloseHandle(semDataRead)
        win32api.CloseHandle(semPythonReady)

    finally:
        shmem.close()

if __name__ == "__main__":
    main()